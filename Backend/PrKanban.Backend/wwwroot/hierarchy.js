'use strict';

(() => {
const t = window.i18n.text;
const form = document.getElementById('details');
const fields = document.getElementById('fields');
const status = document.getElementById('status');
let data = { customers: [], projects: [], deliverables: [] };
let treeTasks = [];
const expandedNodes = new Map();
let hierarchyLoaded = false;
let admin = false;
let canRearrange = false;
let editing = null;
let dirty = false;
let saving = false;
let activation = 0;
let selectedItemKey = null;
let taskPanelReturnItemKey = null;
let draggedItem = null;

function message(text, error = false) {
    status.textContent = text;
    status.classList.toggle('text-red-700', error);
    status.classList.toggle('text-green-700', !error);
}

async function request(path, options = {}) {
    const response = await fetch(`/api/v1/${path}`, { credentials: 'same-origin', ...options });
    if (response.redirected || response.status === 401) throw new Error(t('hierarchy.signIn', 'Bitte anmelden, um die Struktur zu öffnen.'));
    if (!response.ok) {
        const body = (response.headers.get('content-type') || '').includes('json') ? await response.json() : null;
        const conflicts = {
            KEY_EXISTS: t('hierarchy.keyExists', 'Dieses Projektkürzel ist bereits vergeben.'),
            KEY_LOCKED: t('hierarchy.keyLocked', 'Das Projektkürzel kann nicht mehr geändert werden.'),
            KEY_REQUIRED: t('hierarchy.projectKeyRequired', 'Bitte zuerst in Kunden und Projekte ein Projektkürzel hinterlegen.')
        };
        throw new Error(conflicts[body?.code] || body?.detail || Object.values(body?.errors || {}).flat().join(' ') || t('admin.requestFailed', 'Anfrage fehlgeschlagen (HTTP {status}).', {status: response.status}));
    }
    return response.json();
}

function discardChanges() {
    return !dirty || window.confirm(t('hierarchy.discard', 'Ungespeicherte Änderungen verwerfen?'));
}

function button(text, onClick, className = '') {
    const element = document.createElement('button');
    element.type = 'button';
    element.textContent = text;
    element.className = className;
    element.addEventListener('click', onClick);
    return element;
}

function icon(paths) {
    const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    svg.setAttribute('viewBox', '0 0 24 24');
    svg.setAttribute('width', '18');
    svg.setAttribute('height', '18');
    svg.setAttribute('fill', 'none');
    svg.setAttribute('stroke', 'currentColor');
    svg.setAttribute('stroke-width', '2');
    svg.setAttribute('stroke-linecap', 'round');
    svg.setAttribute('stroke-linejoin', 'round');
    svg.setAttribute('aria-hidden', 'true');
    for (const definition of paths) {
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', definition);
        svg.append(path);
    }
    return svg;
}

function action(label, paths, callback) {
    const element = button('', event => {
        event.preventDefault();
        event.stopPropagation();
        callback();
    }, 'tree-action');
    element.title = label;
    element.setAttribute('aria-label', label);
    element.append(icon(paths));
    return element;
}

function entityIcon(kind) {
    const definitions = {
        customers: 'fa-building',
        projects: 'fa-diagram-project',
        deliverables: 'fa-box-open',
        tasks: 'fa-list-check'
    };
    const iconElement = document.createElement('i');
    iconElement.className = `fa-solid ${definitions[kind]}`;
    iconElement.setAttribute('aria-hidden', 'true');
    const container = document.createElement('span');
    container.className = `tree-entity-icon tree-entity-icon--${kind}`;
    container.setAttribute('aria-hidden', 'true');
    container.append(iconElement);
    return container;
}

function treeNode(kind, item, depth, children, childLabel) {
    const node = document.createElement('details');
    const key = `${kind}:${item.id}`;
    node.open = expandedNodes.get(key) ?? true;
    node.addEventListener('toggle', () => expandedNodes.set(key, node.open));
    const row = document.createElement('summary');
    row.className = 'tree-row';
    row.dataset.itemKey = key;
    row.dataset.itemKind = kind;
    row.dataset.itemId = item.id;
    row.dataset.parentId = item.customerId || item.projectId || '';
    row.style.setProperty('--depth', depth);
    row.draggable = canRearrange;
    if (canRearrange) row.title = t('hierarchy.dragItem', 'Ziehen, um zu verschieben');
    const arrow = document.createElement('span');
    arrow.className = children.length ? 'tree-arrow' : 'tree-arrow-placeholder';
    arrow.setAttribute('aria-hidden', 'true');
    const name = button(item.name, event => {
        event.preventDefault();
        event.stopPropagation();
        select(kind, item);
    }, 'tree-name');
    row.addEventListener('click', event => {
        if (event.target.closest('button')) return;
        event.stopPropagation();
        select(kind, item);
    });
    row.append(arrow, entityIcon(kind), name);
    if (kind === 'projects' && item.shortKey) {
        const keyLabel = document.createElement('span');
        keyLabel.className = 'text-xs text-slate-400';
        keyLabel.textContent = item.shortKey;
        row.append(keyLabel);
    }
    if (children.length) {
        const count = document.createElement('span');
        count.className = 'tree-count';
        count.setAttribute('aria-label', `${children.length} ${childLabel}`);
        count.append(icon(['M6 4v5a4 4 0 0 0 4 4h4', 'M6 4a2 2 0 1 0 0 4 2 2 0 0 0 0-4', 'M17 11a3 3 0 1 0 0 6 3 3 0 0 0 0-6']), document.createTextNode(String(children.length)));
        row.append(count);
    }
    if (kind === 'projects') {
        const label = t('projectBoard.openBoard', 'Projektboard öffnen');
        const boardAction = action(`${label}: ${item.name}`, [
            'M3 3h8v8H3z', 'M13 3h8v8h-8z', 'M3 13h8v8H3z', 'M13 13h8v8h-8z'
        ], () => {
            if (!window.viewTabs?.show('kanban')) return;
            window.selectProjectBoard(item.id).then(loaded => {
                if (!loaded) window.viewTabs?.show('projects');
            }).catch(error => {
                console.error('Could not open project board:', error);
                message(t('projectBoard.loadFailed', 'Projektboard konnte nicht geladen werden: {error}', {
                    error: error.message || t('common.unknownError', 'Unbekannter Fehler')
                }), true);
                window.viewTabs?.show('projects');
            });
        });
        boardAction.classList.add('tree-action--board');
        boardAction.append(document.createTextNode('Board'));
        row.append(boardAction);
    }
    const actions = document.createElement('span');
    actions.className = 'tree-actions';
    if (kind === 'deliverables') {
        const label = t('task.create', 'Neuen Task erstellen');
        actions.append(action(`${label}: ${item.name}`, ['M12 5v14', 'M5 12h14'], () => {
            if (prepareTaskEditor(`deliverables:${item.id}`)) window.openHierarchyNewTask(item.id);
        }));
    }
    if (admin && kind !== 'deliverables') {
        const isCustomer = kind === 'customers';
        const label = isCustomer ? t('hierarchy.newProject', 'Projekt anlegen') : t('hierarchy.newDeliverable', 'Lieferobjekt anlegen');
        actions.append(action(`${label}: ${item.name}`, ['M12 5v14', 'M5 12h14'], () => select(isCustomer ? 'projects' : 'deliverables', null, item.id)));
    }
    const edit = admin ? t('common.edit', 'Bearbeiten') : t('hierarchy.view', 'Anzeigen');
    actions.append(action(`${edit}: ${item.name}`, ['M4 20h4l11-11a2.8 2.8 0 0 0-4-4L4 16v4', 'M13 7l4 4'], () => select(kind, item)));
    row.append(actions);
    node.append(row, ...children);
    return node;
}

function taskRow(task) {
    const row = document.createElement('div');
    row.className = 'tree-row';
    row.dataset.itemKey = `tasks:${task.id}`;
    row.dataset.itemKind = 'tasks';
    row.dataset.itemId = task.id;
    row.dataset.parentId = task.deliverableId || '';
    row.style.setProperty('--depth', 3);
    row.draggable = canRearrange;
    if (canRearrange) row.title = t('hierarchy.dragItem', 'Ziehen, um zu verschieben');
    const spacer = document.createElement('span');
    spacer.className = 'tree-arrow-placeholder';
    const taskIcon = entityIcon('tasks');
    const taskTypeColor = task.taskTypeColor || window.getTaskTypeColor?.(task.taskType) || '#64748b';
    taskIcon.style.color = taskTypeColor;
    taskIcon.style.backgroundColor = `${taskTypeColor}20`;
    const openTask = event => {
        event.preventDefault();
        event.stopPropagation();
        if (prepareTaskEditor(`tasks:${task.id}`)) window.openHierarchyTask(task.id);
    };
    const title = button(task.title, openTask, 'tree-name');
    row.addEventListener('click', openTask);
    row.append(spacer, taskIcon, title);
    if (task.isArchived) {
        const archived = document.createElement('span');
        archived.className = 'text-xs text-slate-500';
        archived.textContent = t('board.archive', 'Archiv');
        row.append(archived);
    }
    return row;
}

function renderTree() {
    const tree = document.getElementById('tree');
    tree.replaceChildren();
    if (!hierarchyLoaded) return;
    if (!data.customers.length) {
        const empty = document.createElement('p');
        empty.className = 'text-sm text-slate-500';
        empty.textContent = t('hierarchy.empty', 'Noch keine Kunden vorhanden.');
        tree.append(empty);
    }
    for (const customer of data.customers) {
        const projects = data.projects.filter(project => project.customerId === customer.id);
        const projectNodes = projects.map(project => {
            const deliverables = data.deliverables.filter(deliverable => deliverable.projectId === project.id);
            const deliverableNodes = deliverables.map(deliverable => {
                const tasks = treeTasks.filter(task => task.deliverableId === deliverable.id)
                    .sort((a, b) => (a.hierarchyOrder ?? 0) - (b.hierarchyOrder ?? 0));
                return treeNode('deliverables', deliverable, 2, tasks.map(taskRow), t('hierarchy.tasks', 'Tasks'));
            });
            return treeNode('projects', project, 1, deliverableNodes, t('hierarchy.deliverables', 'Lieferobjekte'));
        });
        tree.append(treeNode('customers', customer, 0, projectNodes, t('hierarchy.projects', 'Projekte')));
    }
    tree.querySelectorAll('[data-item-key]').forEach(row => {
        row.classList.toggle('is-selected', row.dataset.itemKey === selectedItemKey);
    });
}

function siblingsFor(kind, parentId) {
    if (kind === 'customers') return data.customers;
    if (kind === 'projects') return data.projects.filter(item => item.customerId === parentId);
    if (kind === 'deliverables') return data.deliverables.filter(item => item.projectId === parentId);
    if (kind === 'tasks') return treeTasks.filter(item => item.deliverableId === parentId)
        .sort((a, b) => (a.hierarchyOrder ?? 0) - (b.hierarchyOrder ?? 0));
    return [];
}

function getDropPlan(source, row, clientY) {
    const targetKind = row.dataset.itemKind;
    const targetId = row.dataset.itemId;
    const targetParentId = row.dataset.parentId || null;
    if (!targetKind || !targetId) return null;

    if (source.kind === targetKind) {
        if (source.id === targetId) return null;
        const siblings = siblingsFor(source.kind, targetParentId);
        const targetIndex = siblings.findIndex(item => item.id === targetId);
        const sourceIndex = siblings.findIndex(item => item.id === source.id);
        if (targetIndex < 0) return null;
        const before = clientY < row.getBoundingClientRect().top + row.getBoundingClientRect().height / 2;
        let index = targetIndex + (before ? 0 : 1);
        if (source.parentId === targetParentId && sourceIndex >= 0) {
            if (sourceIndex < index) index--;
            if (sourceIndex === index) return null;
        }
        return { parentId: targetParentId, index, marker: before ? 'drop-before' : 'drop-after' };
    }

    const parentKind = { projects: 'customers', deliverables: 'projects', tasks: 'deliverables' };
    if (parentKind[source.kind] !== targetKind) return null;
    const siblings = siblingsFor(source.kind, targetId);
    if (source.parentId === targetId) {
        const sourceIndex = siblings.findIndex(item => item.id === source.id);
        if (sourceIndex === siblings.length - 1) return null;
        return { parentId: targetId, index: siblings.length - 1, marker: 'drop-inside' };
    }
    return { parentId: targetId, index: siblings.length, marker: 'drop-inside' };
}

function clearDropIndicators() {
    document.querySelectorAll('#tree .drop-before, #tree .drop-after, #tree .drop-inside')
        .forEach(row => row.classList.remove('drop-before', 'drop-after', 'drop-inside'));
}

async function moveItem(source, plan) {
    if (!discardChanges()) return;
    dirty = false;
    const requestId = activation;
    try {
        await request(`hierarchy/${source.kind}/${encodeURIComponent(source.id)}/position`, {
            method: 'PUT',
            headers: {'Content-Type': 'application/json'},
            body: JSON.stringify({parentId: plan.parentId, index: plan.index})
        });
        const parentKind = { projects: 'customers', deliverables: 'projects', tasks: 'deliverables' }[source.kind];
        if (parentKind && plan.parentId) expandedNodes.set(`${parentKind}:${plan.parentId}`, true);
        const [nextData, nextTasks] = await Promise.all([request('hierarchy'), loadTreeTasks()]);
        if (requestId !== activation) return;
        data = nextData;
        treeTasks = nextTasks;
        hierarchyLoaded = true;
        renderTree();
        if (source.kind === 'projects') window.refreshProjectBoardOptions?.();
        if (source.kind === 'tasks' && source.parentId !== plan.parentId) {
            const movedTask = treeTasks.find(task => task.id === source.id);
            if (!movedTask) throw new Error(t('hierarchy.taskNotFound', 'Task konnte im Kanban nicht gefunden werden.'));
            await window.syncMovedHierarchyTask(movedTask);
        }
        if (editing?.kind === source.kind && editing.id === source.id && source.kind !== 'tasks') {
            const collection = source.kind === 'customers' ? data.customers :
                source.kind === 'projects' ? data.projects : data.deliverables;
            const moved = collection.find(item => item.id === source.id);
            if (moved) select(source.kind, moved);
        }
        message(t('hierarchy.moved', 'Position aktualisiert.'));
    } catch (error) {
        console.error('Hierarchy move failed:', error);
        if (requestId === activation) message(error.message, true);
    }
}

const tree = document.getElementById('tree');
tree.addEventListener('dragstart', event => {
    const row = event.target.closest('.tree-row[draggable="true"]');
    if (!canRearrange || !row) {
        event.preventDefault();
        return;
    }
    draggedItem = {
        kind: row.dataset.itemKind,
        id: row.dataset.itemId,
        parentId: row.dataset.parentId || null
    };
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', `${draggedItem.kind}:${draggedItem.id}`);
    row.classList.add('is-dragging');
});
tree.addEventListener('dragover', event => {
    if (!canRearrange || !draggedItem) return;
    const row = event.target.closest('.tree-row');
    const plan = row && getDropPlan(draggedItem, row, event.clientY);
    clearDropIndicators();
    if (!plan) return;
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
    row.classList.add(plan.marker);
});
tree.addEventListener('drop', async event => {
    if (!canRearrange || !draggedItem) return;
    const row = event.target.closest('.tree-row');
    const plan = row && getDropPlan(draggedItem, row, event.clientY);
    if (!plan) return;
    event.preventDefault();
    const source = draggedItem;
    draggedItem = null;
    clearDropIndicators();
    document.querySelectorAll('#tree .is-dragging').forEach(item => item.classList.remove('is-dragging'));
    await moveItem(source, plan);
});
tree.addEventListener('dragend', () => {
    draggedItem = null;
    clearDropIndicators();
    document.querySelectorAll('#tree .is-dragging').forEach(item => item.classList.remove('is-dragging'));
});

async function loadTreeTasks() {
    const tasks = [];
    let total;
    do {
        const result = await request(`tasks?skip=${tasks.length}&take=100`);
        const page = result.data;
        if (!Array.isArray(page?.tasks) || !Number.isInteger(page?.pagination?.total))
            throw new Error(t('hierarchy.loadFailed', 'Struktur konnte nicht geladen werden.'));
        total = page.pagination.total;
        if (!page.tasks.length && tasks.length < total)
            throw new Error(t('hierarchy.loadFailed', 'Struktur konnte nicht geladen werden.'));
        tasks.push(...page.tasks);
    } while (tasks.length < total);
    return tasks;
}

function select(kind, item = null, parentId = '') {
    if (kind === 'deliverables' && !item && !data.projects.find(project => project.id === parentId)?.shortKey) {
        message(t('hierarchy.projectKeyRequired', 'Bitte zuerst in Kunden und Projekte ein Projektkürzel hinterlegen.'), true);
        return;
    }
    if (!discardChanges()) return;
    if (window.viewTabs.current() === 'projects' && !document.getElementById('task-details-panel').classList.contains('hidden'))
        window.closeDetailsPanel();
    dirty = false;
    selectedItemKey = item ? `${kind}:${item.id}` : null;
    document.querySelectorAll('#tree [data-item-key]').forEach(row => {
        row.classList.toggle('is-selected', row.dataset.itemKey === selectedItemKey);
    });
    editing = {kind, id: item?.id || null};
    form.reset();
    message('');
    document.getElementById('placeholder').classList.add('hidden');
    form.classList.remove('hidden');
    document.getElementById('read-only').classList.toggle('hidden', admin);
    document.getElementById('form-title').textContent = kind === 'customers' ? t('hierarchy.customer', 'Kunde') : kind === 'projects' ? t('hierarchy.project', 'Projekt') : t('hierarchy.deliverable', 'Lieferobjekt');
    document.getElementById('name-label').textContent = kind === 'customers' ? t('hierarchy.companyName', 'Firmenname') : kind === 'projects' ? t('hierarchy.projectName', 'Projektname') : t('hierarchy.deliverableTitle', 'Titel');
    document.getElementById('item-id').textContent = item ? `ID: ${item.id}` : '';
    form.querySelectorAll('[data-for]').forEach(label => {
        const active = label.dataset.for.split(' ').includes(kind);
        label.classList.toggle('hidden', !active);
        label.querySelectorAll('input,select').forEach(input => { input.disabled = !active; input.required = false; });
    });
    fields.disabled = !admin;
    document.getElementById('save').classList.toggle('hidden', !admin);
    form.elements.name.value = item?.name || '';
    const parent = form.elements.parent;
    parent.replaceChildren();
    const choices = kind === 'projects' ? data.customers : kind === 'deliverables' ? data.projects : [];
    for (const choice of choices) parent.add(new Option(choice.name, choice.id));
    if (kind !== 'customers') {
        parent.required = true;
        parent.value = item?.customerId || item?.projectId || parentId;
        document.getElementById('parent-label').textContent = kind === 'projects' ? t('hierarchy.customer', 'Kunde') : t('hierarchy.project', 'Projekt');
    }
    for (const name of ['debitorNo','crmUrl','crmLink','shortKey','internalOrderNumber','customerOrderNumber','defaultAssignee'])
        form.elements[name].value = item?.[name] || '';
    form.elements.shortKey.required = kind === 'projects';
    form.elements.shortKey.readOnly = !!item?.shortKey;
    for (const name of ['initialEstimateHours','budgetHours','actualHours']) {
        form.elements[name].value = item?.[name] ?? 0;
        form.elements[name].required = kind === 'deliverables';
    }
    if (admin) form.elements.name.focus();
}

form.addEventListener('input', () => { dirty = true; });
window.addEventListener('beforeunload', event => { if (dirty) { event.preventDefault(); event.returnValue = ''; } });
document.getElementById('new-customer').addEventListener('click', () => select('customers'));

form.addEventListener('submit', async event => {
    event.preventDefault();
    const {kind, id} = editing;
    const body = {name: form.elements.name.value.trim()};
    if (!body.name) { form.elements.name.setCustomValidity(t('hierarchy.nameRequired', 'Bitte einen Namen eingeben.')); form.elements.name.reportValidity(); form.elements.name.setCustomValidity(''); return; }
    const copy = names => names.forEach(name => { body[name] = form.elements[name].value.trim() || null; });
    if (kind === 'customers') copy(['debitorNo', 'crmUrl']);
    if (kind === 'projects') {
        body.customerId = form.elements.parent.value;
        copy(['shortKey','crmLink','crmUrl','internalOrderNumber','customerOrderNumber','defaultAssignee']);
    }
    if (kind === 'deliverables') {
        body.projectId = form.elements.parent.value;
        for (const name of ['initialEstimateHours','budgetHours','actualHours']) body[name] = Number(form.elements[name].value);
    }
    const submit = document.getElementById('save');
    submit.disabled = true;
    saving = true;
    const requestId = activation;
    try {
        const saved = await request(`hierarchy/${kind}${id ? `/${encodeURIComponent(id)}` : ''}`, {
            method: id ? 'PUT' : 'POST',
            headers: {'Content-Type':'application/json'},
            body: JSON.stringify(body)
        });
        if (requestId !== activation) return;
        dirty = false;
        editing.id = saved.id;
        document.getElementById('item-id').textContent = `ID: ${saved.id}`;
        if (kind === 'projects') form.elements.shortKey.readOnly = true;
        const [nextData, nextTasks] = await Promise.all([request('hierarchy'), loadTreeTasks()]);
        if (requestId !== activation) return;
        data = nextData;
        treeTasks = nextTasks;
        hierarchyLoaded = true;
        renderTree();
        if (kind === 'projects') window.refreshProjectBoardOptions?.();
        select(kind, saved);
        message(t('hierarchy.saved', 'Gespeichert.'));
    } catch (error) {
        console.error('Hierarchy save failed:', error);
        if (requestId === activation) message(error.message, true);
    } finally { submit.disabled = false; saving = false; }
});

window.addEventListener('languagechange', () => { renderTree(); });
window.addEventListener('tasktypecolorschanged', () => { renderTree(); });

async function activate() {
    const requestId = ++activation;
    await window.i18n.ready;
    if (requestId !== activation) return;
    hierarchyLoaded = false;
    data = { customers: [], projects: [], deliverables: [] };
    treeTasks = [];
    admin = false;
    renderTree();
    document.getElementById('new-customer').classList.add('hidden');
    message(t('admin.loading', 'Informationen werden geladen …'));
    try {
        const current = await request('auth/user');
        if (requestId !== activation) return;
        admin = !!current.data?.isAdmin;
        canRearrange = true;
        document.getElementById('new-customer').classList.toggle('hidden', !admin);
        const [nextData, nextTasks] = await Promise.all([request('hierarchy'), loadTreeTasks()]);
        if (requestId !== activation) return;
        data = nextData;
        treeTasks = nextTasks;
        hierarchyLoaded = true;
        renderTree();
        window.refreshProjectBoardOptions?.();
        message('');
    } catch (error) {
        if (requestId !== activation) return;
        console.error('Hierarchy load failed:', error);
        message(error.message, true);
    }
}

function reset() {
    dirty = false;
    activation++;
    hierarchyLoaded = false;
    data = { customers: [], projects: [], deliverables: [] };
    treeTasks = [];
    admin = false;
    canRearrange = false;
    editing = null;
    selectedItemKey = null;
    taskPanelReturnItemKey = null;
    renderTree();
    form.reset();
    form.classList.add('hidden');
    document.getElementById('placeholder').classList.remove('hidden');
    document.getElementById('read-only').classList.add('hidden');
    document.getElementById('new-customer').classList.add('hidden');
    message('');
}

async function refreshTasks() {
    const requestId = activation;
    try {
        const nextTasks = await loadTreeTasks();
        if (requestId !== activation || !hierarchyLoaded) return false;
        treeTasks = nextTasks;
        renderTree();
        return true;
    } catch (error) {
        console.error('Task tree refresh failed:', error);
        if (requestId === activation) message(error.message || t('hierarchy.loadFailed', 'Struktur konnte nicht geladen werden.'), true);
        return false;
    }
}

function updateSelectedTreeRows() {
    document.querySelectorAll('#tree [data-item-key]').forEach(row => {
        row.classList.toggle('is-selected', row.dataset.itemKey === selectedItemKey);
    });
}

function prepareTaskEditor(itemKey) {
    if (!discardChanges()) return false;
    dirty = false;
    if (itemKey.startsWith('deliverables:')) taskPanelReturnItemKey = itemKey;
    else if (!selectedItemKey?.startsWith('tasks:')) taskPanelReturnItemKey = selectedItemKey;
    selectedItemKey = itemKey;
    updateSelectedTreeRows();
    return true;
}

function returnFromTaskEditor() {
    selectedItemKey = taskPanelReturnItemKey;
    taskPanelReturnItemKey = null;
    updateSelectedTreeRows();
}

function leave() {
    if (saving) {
        message(t('admin.loading', 'Informationen werden geladen …'));
        return false;
    }
    if (!discardChanges()) return false;
    reset();
    return true;
}

window.hierarchyView = { activate, leave, reset, refreshTasks, returnFromTaskEditor };
})();
