'use strict';

const t = window.i18n.text;
const statusMessage = document.getElementById('status');
const form = document.getElementById('user-form');
const userList = document.getElementById('user-list');
const userPanel = document.getElementById('user-panel');
const panelResizer = document.getElementById('user-panel-resizer');
const panelStatus = document.getElementById('user-panel-status');
let users = [];
let translations = [];
let taskTypes = {};
let tagsConfig = {};
const translationDrafts = new Map();
const pendingTranslations = new Set();
let panelOpener = null;
let activeView = 'users';

class FeatureUnavailableError extends Error {}

async function request(path, options = {}) {
    const response = await fetch(`/api/v1/admin/${path}`, {
        credentials: 'same-origin',
        ...options,
        headers: options.body ? { 'Content-Type': 'application/json' } : {}
    });
    if (response.redirected || response.status === 401)
        throw new Error(t('admin.signInRequired', 'Bitte als Administrator anmelden.'));
    if (response.status === 403)
        throw new Error(t('admin.adminOnly', 'Nur Administratoren können diesen Bereich öffnen.'));
    if (!response.ok) {
        const contentType = response.headers.get('content-type') || '';
        const error = contentType.includes('json') ? await response.json() : null;
        const validation = error?.errors ? Object.values(error.errors).flat().join(' ') : '';
        throw new Error(error?.detail || validation || t('admin.requestFailed', 'Anfrage fehlgeschlagen (HTTP {status}).', { status: response.status }));
    }
    if (response.status === 204) return null;
    if (!(response.headers.get('content-type') || '').includes('json'))
        throw new FeatureUnavailableError(t('admin.featureUnavailable', 'Diese Funktion ist auf dem laufenden Server noch nicht verfügbar.'));
    return response.json();
}

function showStatus(message, isError = false) {
    statusMessage.textContent = message;
    statusMessage.classList.toggle('text-red-700', isError);
    statusMessage.classList.toggle('hidden', !message);
    panelStatus.textContent = isError ? message : '';
    panelStatus.classList.toggle('hidden', !isError || !message);
}

function closeUserPanel() {
    userPanel.classList.add('hidden');
    userPanel.classList.remove('flex');
    panelResizer.classList.add('hidden');
    if (panelOpener?.isConnected) panelOpener.focus();
    panelOpener = null;
}

function renderUsers() {
    const search = document.getElementById('user-search').value.trim().toLocaleLowerCase();
    const filtered = users.filter(user => [user.name, user.email, user.role].some(value => value.toLocaleLowerCase().includes(search)));
    userList.replaceChildren();
    document.getElementById('empty-users').classList.toggle('hidden', filtered.length !== 0);
    for (const user of filtered) {
        const row = document.createElement('tr');
        row.className = 'border-t border-slate-100';
        const identity = document.createElement('td');
        identity.className = 'p-4';
        const name = document.createElement('div');
        name.className = 'font-semibold';
        name.textContent = user.name;
        const email = document.createElement('div');
        email.className = 'text-xs text-slate-500';
        email.textContent = user.email;
        identity.append(name, email);
        const role = document.createElement('td');
        role.className = 'p-4';
        role.textContent = user.role;
        const active = document.createElement('td');
        active.className = 'p-4';
        active.textContent = user.active ? t('common.active', 'Aktiv') : t('common.inactive', 'Inaktiv');
        const action = document.createElement('td');
        action.className = 'p-4';
        const edit = document.createElement('button');
        edit.type = 'button';
        edit.className = 'text-blue-700 font-semibold hover:underline';
        edit.textContent = t('common.edit', 'Bearbeiten');
        edit.addEventListener('click', () => editUser(user));
        action.append(edit);
        row.append(identity, role, active, action);
        userList.append(row);
    }
}

function renderTranslations() {
    const search = document.getElementById('translation-search').value.trim().toLocaleLowerCase();
    const filtered = translations.filter(item => {
        const draft = translationDrafts.get(item.key) || item;
        return [item.key, draft.category, draft.german, draft.english]
            .some(value => value.toLocaleLowerCase().includes(search));
    });
    const list = document.getElementById('translation-list');
    list.replaceChildren();
    document.getElementById('empty-translations').classList.toggle('hidden', filtered.length !== 0);
    for (const item of filtered) {
        const draft = translationDrafts.get(item.key) || { ...item };
        const row = document.createElement('tr');
        row.className = 'border-t border-slate-100';
        const keyCell = document.createElement('td');
        keyCell.className = 'p-4';
        const key = document.createElement('code');
        key.textContent = item.key;
        const category = document.createElement('input');
        category.value = draft.category;
        category.maxLength = 100;
        category.required = true;
        category.setAttribute('aria-label', `${t('admin.category', 'Kategorie')} ${item.key}`);
        category.className = 'block mt-1 w-full rounded-lg border border-slate-200 px-2 py-1 text-xs';
        keyCell.append(key, category);
        const inputs = {};
        for (const [field, label] of [['german', t('admin.german', 'Deutsch')], ['english', t('admin.english', 'Englisch')]]) {
            const cell = document.createElement('td');
            cell.className = 'p-3';
            const input = document.createElement('input');
            input.value = draft[field];
            input.required = true;
            input.maxLength = 2048;
            input.setAttribute('aria-label', `${label} ${item.key}`);
            input.className = 'w-full min-w-[160px] rounded-lg border border-slate-200 px-2 py-1 text-sm';
            inputs[field] = input;
            input.addEventListener('input', () => {
                draft[field] = input.value;
                translationDrafts.set(item.key, draft);
            });
            cell.append(input);
            row.append(cell);
        }
        row.prepend(keyCell);
        category.addEventListener('input', () => {
            draft.category = category.value;
            translationDrafts.set(item.key, draft);
        });
        const actions = document.createElement('td');
        actions.className = 'p-4 whitespace-nowrap';
        const save = document.createElement('button');
        save.type = 'button';
        save.className = 'text-blue-700 font-semibold hover:underline';
        save.textContent = t('common.save', 'Speichern');
        save.disabled = pendingTranslations.has(item.key);
        save.addEventListener('click', async () => {
            if (![category, inputs.german, inputs.english].every(input => input.reportValidity())) return;
            pendingTranslations.add(item.key);
            save.disabled = true;
            remove.disabled = true;
            const submitted = { category: category.value, german: inputs.german.value, english: inputs.english.value };
            try {
                const updated = await request(`translations/${encodeURIComponent(item.key)}`, {
                    method: 'PUT',
                    body: JSON.stringify(Object.fromEntries(Object.entries(submitted).map(([field, value]) => [field, value.trim()])))
                });
                const current = translations.find(entry => entry.key === item.key);
                if (current) Object.assign(current, updated);
                const currentDraft = translationDrafts.get(item.key);
                if (currentDraft && ['category', 'german', 'english'].every(field => currentDraft[field] === submitted[field]))
                    translationDrafts.delete(item.key);
                renderTranslations();
                showStatus(t('admin.translationSaved', 'Übersetzung gespeichert.'));
            } catch (error) {
                console.error('Übersetzung konnte nicht gespeichert werden:', error);
                showStatus(error.message, true);
            } finally {
                pendingTranslations.delete(item.key);
                renderTranslations();
            }
        });
        const remove = document.createElement('button');
        remove.type = 'button';
        remove.className = 'ml-3 text-red-700 hover:underline';
        remove.textContent = t('common.delete', 'Löschen');
        remove.disabled = pendingTranslations.has(item.key);
        remove.addEventListener('click', async () => {
            if (!window.confirm(t('admin.confirmDeleteKey', 'Schlüssel "{key}" löschen?', { key: item.key }))) return;
            pendingTranslations.add(item.key);
            remove.disabled = true;
            save.disabled = true;
            try {
                await request(`translations/${encodeURIComponent(item.key)}`, { method: 'DELETE' });
                translations = translations.filter(entry => entry.key !== item.key);
                translationDrafts.delete(item.key);
                renderTranslations();
                showStatus(t('admin.translationDeleted', 'Übersetzung gelöscht.'));
            } catch (error) {
                console.error('Übersetzung konnte nicht gelöscht werden:', error);
                showStatus(error.message, true);
            } finally {
                pendingTranslations.delete(item.key);
                renderTranslations();
            }
        });
        actions.append(save, remove);
        row.append(actions);
        list.append(row);
    }
}

function defaultTaskTypes() {
    return {
        'Installation': { path: ['ToDo', 'InProgress', 'Done'], color: '#10b981' },
        'Typicals creation': { path: ['ToDo', 'Analysis', 'InProgress', 'Done'], color: '#f97316' }
    };
}

function loadTaskTypes() {
    const stored = localStorage.getItem('kanban_workflowPaths');
    const parsed = stored === null ? defaultTaskTypes() : JSON.parse(stored);
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
        throw new Error(t('admin.invalidTaskTypes', 'Die gespeicherte Task-Typ-Konfiguration ist ungültig.'));

    return Object.fromEntries(Object.entries(parsed).map(([name, config]) => [
        name,
        {
            path: Array.isArray(config?.path) ? config.path : Array.isArray(config) ? config : ['ToDo', 'Done'],
            color: typeof config?.color === 'string' ? config.color : '#64748b'
        }
    ]));
}

function saveTaskTypes() {
    localStorage.setItem('kanban_workflowPaths', JSON.stringify(taskTypes));
}

async function requestTask(path, options = {}) {
    const response = await fetch(`/api/v1/tasks${path}`, {
        credentials: 'same-origin',
        ...options,
        headers: options.body ? { 'Content-Type': 'application/json' } : {}
    });
    if (response.redirected || response.status === 401)
        throw new Error(t('admin.signInRequired', 'Bitte als Administrator anmelden.'));
    if (!response.ok) {
        const contentType = response.headers.get('content-type') || '';
        const error = contentType.includes('json') ? await response.json() : null;
        throw new Error(error?.detail || error?.title || error?.error?.message ||
            t('admin.requestFailed', 'Anfrage fehlgeschlagen (HTTP {status}).', { status: response.status }));
    }
    if (response.status === 204) return null;
    return response.json();
}

async function getAllTasks() {
    const tasks = [];
    const pageSize = 100;
    let skip = 0;
    while (true) {
        const response = await requestTask(`?skip=${skip}&take=${pageSize}`);
        const pageTasks = response?.data?.tasks || response?.tasks;
        if (!Array.isArray(pageTasks))
            throw new Error(t('admin.invalidTasksResponse', 'Die Task-Liste konnte nicht gelesen werden.'));
        tasks.push(...pageTasks);
        const total = response?.data?.pagination?.total ?? response?.pagination?.total;
        if (pageTasks.length < pageSize || (Number.isFinite(total) && tasks.length >= total)) break;
        skip += pageTasks.length;
    }
    return tasks;
}

function saveTaskTypeFilters(renameFrom = null, renameTo = null, remove = null) {
    const stored = localStorage.getItem('kanban_selectedFilterTypes');
    if (stored === null) return;
    const selected = JSON.parse(stored);
    if (!Array.isArray(selected))
        throw new Error(t('admin.invalidTaskTypes', 'Die gespeicherte Task-Typ-Konfiguration ist ungültig.'));
    const updated = selected
        .filter(type => type !== remove)
        .map(type => type === renameFrom ? renameTo : type);
    localStorage.setItem('kanban_selectedFilterTypes', JSON.stringify([...new Set(updated)]));
}

function renderTaskTypes() {
    const list = document.getElementById('types-manager-list');
    const entries = Object.entries(taskTypes);
    list.replaceChildren();
    document.getElementById('empty-types').classList.toggle('hidden', entries.length !== 0);

    const workflowColumns = [
        ['ToDo', 'board.todo', 'To Do', true],
        ['Analysis', 'board.analysis', 'Analysis', false],
        ['InProgress', 'board.inProgress', 'In Progress', false],
        ['Done', 'board.done', 'Done', true]
    ];
    for (const [type, config] of entries) {
        const row = document.createElement('div');
        row.className = 'rounded-xl border border-slate-200 bg-white p-4 space-y-2';
        const controls = document.createElement('div');
        controls.className = 'flex flex-wrap items-center gap-3';
        const name = document.createElement('input');
        name.type = 'text';
        name.value = type;
        name.maxLength = 100;
        name.setAttribute('aria-label', t('admin.taskTypeName', 'Task-Typ-Name'));
        name.className = 'min-w-[180px] flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold';
        name.addEventListener('keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
                name.blur();
            }
        });
        name.addEventListener('blur', () => renameTaskType(type, name.value, name));
        const color = document.createElement('input');
        color.type = 'color';
        color.value = config.color;
        color.setAttribute('aria-label', t('common.color', 'Farbe'));
        color.className = 'h-8 w-10 cursor-pointer';
        color.addEventListener('change', () => updateTaskTypeColor(type, color.value));
        const remove = document.createElement('button');
        remove.type = 'button';
        remove.textContent = t('common.delete', 'Löschen');
        remove.className = 'rounded-lg px-3 py-2 text-sm font-semibold text-red-700 hover:bg-red-50';
        remove.addEventListener('click', () => deleteTaskType(type));
        controls.append(name, color, remove);
        const workflow = document.createElement('fieldset');
        workflow.className = 'flex flex-wrap gap-4 text-xs text-slate-600';
        const legend = document.createElement('legend');
        legend.className = 'mb-2 text-xs text-slate-500';
        legend.textContent = t('types.workflowPath', 'Erlaubter Workflow-Pfad (Spalten)');
        workflow.append(legend);
        for (const [column, labelKey, fallback, fixed] of workflowColumns) {
            const label = document.createElement('label');
            label.className = 'inline-flex items-center gap-1.5';
            const checkbox = document.createElement('input');
            checkbox.type = 'checkbox';
            checkbox.dataset.column = column;
            checkbox.checked = fixed || config.path.includes(column);
            checkbox.disabled = fixed;
            checkbox.className = 'accent-blue-600';
            checkbox.addEventListener('change', () => updateTaskTypePath(type, workflow));
            const text = document.createElement('span');
            text.textContent = t(labelKey, fallback);
            label.append(checkbox, text);
            workflow.append(label);
        }
        row.append(controls, workflow);
        list.append(row);
    }
}

function updateTaskTypePath(type, workflow) {
    const previousPath = taskTypes[type].path;
    taskTypes[type].path = [...workflow.querySelectorAll('input[data-column]:checked')]
        .map(checkbox => checkbox.dataset.column);
    try {
        saveTaskTypes();
        showStatus(t('admin.taskTypeSaved', 'Task-Typ gespeichert.'));
    } catch (error) {
        console.error('Task-Typ-Workflow konnte nicht gespeichert werden:', error);
        taskTypes[type].path = previousPath;
        showStatus(error.message, true);
        renderTaskTypes();
    }
}

const legacySampleTags = {
    'Prio A': '#ef4444',
    Database: '#3b82f6',
    Konzept: '#a855f7',
    Hardware: '#f97316',
    Network: '#06b6d4',
    Freigabe: '#10b981',
    Review: '#eab308',
    Doku: '#64748b'
};

function loadTags() {
    const stored = localStorage.getItem('kanban_tagsConfig');
    const parsed = stored === null ? {} : JSON.parse(stored);
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed))
        throw new Error(t('admin.invalidTags', 'Die gespeicherte Tag-Konfiguration ist ungültig.'));
    const entries = Object.entries(parsed);
    const isLegacyDefaults = entries.length === Object.keys(legacySampleTags).length &&
        entries.every(([name, color]) => legacySampleTags[name] === color);
    if (isLegacyDefaults) return {};
    return Object.fromEntries(entries.map(([name, color]) => [name, typeof color === 'string' ? color : '#64748b']));
}

function saveTags() {
    localStorage.setItem('kanban_tagsConfig', JSON.stringify(tagsConfig));
}

function saveTagFilters(renameFrom = null, renameTo = null, remove = null, preserveOld = false) {
    const stored = localStorage.getItem('kanban_selectedFilterTags');
    if (stored === null) return;
    const selected = JSON.parse(stored);
    if (!Array.isArray(selected))
        throw new Error(t('admin.invalidTags', 'Die gespeicherte Tag-Konfiguration ist ungültig.'));
    const updated = selected
        .filter(tag => tag !== remove)
        .map(tag => tag === renameFrom ? renameTo : tag);
    if (preserveOld && selected.includes(renameFrom)) updated.push(renameFrom);
    localStorage.setItem('kanban_selectedFilterTags', JSON.stringify([...new Set(updated)]));
}

function renderTags() {
    const list = document.getElementById('tag-manager-list');
    const entries = Object.entries(tagsConfig);
    list.replaceChildren();
    document.getElementById('empty-tags').classList.toggle('hidden', entries.length !== 0);

    for (const [tag, tagColor] of entries) {
        const row = document.createElement('div');
        row.className = 'rounded-xl border border-slate-200 bg-white p-4';
        const controls = document.createElement('div');
        controls.className = 'flex flex-wrap items-center gap-3';
        const name = document.createElement('input');
        name.type = 'text';
        name.value = tag;
        name.maxLength = 100;
        name.setAttribute('aria-label', t('admin.tagName', 'Tag-Name'));
        name.className = 'min-w-[180px] flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold';
        name.addEventListener('keydown', event => {
            if (event.key === 'Enter') {
                event.preventDefault();
                name.blur();
            }
        });
        name.addEventListener('blur', () => renameTag(tag, name.value, name));
        const color = document.createElement('input');
        color.type = 'color';
        color.value = tagColor;
        color.setAttribute('aria-label', t('common.color', 'Farbe'));
        color.className = 'h-8 w-10 cursor-pointer';
        color.addEventListener('change', () => updateTagColor(tag, color.value));
        const remove = document.createElement('button');
        remove.type = 'button';
        remove.textContent = t('common.delete', 'Löschen');
        remove.className = 'rounded-lg px-3 py-2 text-sm font-semibold text-red-700 hover:bg-red-50';
        remove.addEventListener('click', () => deleteTag(tag));
        controls.append(name, color, remove);
        row.append(controls);
        list.append(row);
    }
}

function updateTagColor(tag, color) {
    try {
        tagsConfig[tag] = color;
        saveTags();
        showStatus(t('admin.tagSaved', 'Tag gespeichert.'));
    } catch (error) {
        console.error('Tag-Farbe konnte nicht gespeichert werden:', error);
        showStatus(error.message, true);
        tagsConfig = loadTags();
        renderTags();
    }
}

async function renameTag(oldTag, value, input) {
    const newTag = value.trim();
    if (!newTag || newTag === oldTag) {
        input.value = oldTag;
        return;
    }
    if (Object.prototype.hasOwnProperty.call(tagsConfig, newTag)) {
        showStatus(t('tags.duplicateName', 'Tag-Name existiert bereits.'), true);
        input.value = oldTag;
        return;
    }

    try {
        const tasks = (await getAllTasks()).filter(task => Array.isArray(task.tags) && task.tags.includes(oldTag));
        tagsConfig[newTag] = tagsConfig[oldTag] || '#475569';
        saveTags();
        const results = await Promise.allSettled(tasks.map(task =>
            requestTask(`/${encodeURIComponent(task.id)}`, {
                method: 'PUT',
                body: JSON.stringify({ tags: [...new Set(task.tags.map(tag => tag === oldTag ? newTag : tag))] })
            })
        ));
        if (results.some(result => result.status === 'rejected')) {
            saveTagFilters(oldTag, newTag, null, true);
            showStatus(t('admin.partialTagRename', 'Einige Tasks konnten nicht aktualisiert werden. Beide Tags bleiben erhalten.'), true);
        } else {
            delete tagsConfig[oldTag];
            saveTags();
            saveTagFilters(oldTag, newTag);
            showStatus(t('tags.renamed', 'Tag "{old}" wurde in "{new}" umbenannt.', { old: oldTag, new: newTag }));
        }
        renderTags();
    } catch (error) {
        console.error('Tag konnte nicht umbenannt werden:', error);
        showStatus(error.message, true);
        tagsConfig = loadTags();
        renderTags();
    }
}

async function deleteTag(tag) {
    try {
        const tasks = (await getAllTasks()).filter(task => Array.isArray(task.tags) && task.tags.includes(tag));
        const results = await Promise.allSettled(tasks.map(task =>
            requestTask(`/${encodeURIComponent(task.id)}`, {
                method: 'PUT',
                body: JSON.stringify({ tags: task.tags.filter(value => value !== tag) })
            })
        ));
        if (results.some(result => result.status === 'rejected')) {
            showStatus(t('admin.partialTagDelete', 'Einige Tasks konnten nicht aktualisiert werden. Der Tag bleibt erhalten.'), true);
            return;
        }
        const deletedColor = tagsConfig[tag];
        delete tagsConfig[tag];
        try {
            saveTags();
        } catch (error) {
            tagsConfig[tag] = deletedColor;
            throw error;
        }
        saveTagFilters(null, null, tag);
        renderTags();
        showStatus(t('admin.tagDeleted', 'Tag und Task-Zuweisungen gelöscht.'));
    } catch (error) {
        console.error('Tag konnte nicht gelöscht werden:', error);
        showStatus(error.message, true);
    }
}

document.getElementById('tag-form').addEventListener('submit', event => {
    event.preventDefault();
    const nameInput = document.getElementById('new-tag-name');
    const name = nameInput.value.trim();
    if (!name) return;
    if (Object.prototype.hasOwnProperty.call(tagsConfig, name)) {
        showStatus(t('tags.duplicateName', 'Tag-Name existiert bereits.'), true);
        return;
    }
    tagsConfig[name] = document.getElementById('new-tag-color').value;
    try {
        saveTags();
        nameInput.value = '';
        renderTags();
        showStatus(t('admin.tagSaved', 'Tag gespeichert.'));
    } catch (error) {
        console.error('Tag konnte nicht gespeichert werden:', error);
        delete tagsConfig[name];
        showStatus(error.message, true);
    }
});

async function renameTaskType(oldType, value, input) {
    const newType = value.trim();
    if (!newType || newType === oldType) {
        input.value = oldType;
        return;
    }
    if (Object.prototype.hasOwnProperty.call(taskTypes, newType)) {
        showStatus(t('types.duplicateName', 'Typ-Name existiert bereits.'), true);
        input.value = oldType;
        return;
    }

    try {
        const tasks = (await getAllTasks()).filter(task => task.taskType === oldType);
        const oldConfig = taskTypes[oldType];
        taskTypes[newType] = oldConfig;
        saveTaskTypes();

        const results = await Promise.allSettled(tasks.map(task =>
            requestTask(`/${encodeURIComponent(task.id)}`, {
                method: 'PUT',
                body: JSON.stringify({ taskType: newType })
            })
        ));
        const failed = results.some(result => result.status === 'rejected');
        if (!failed) {
            delete taskTypes[oldType];
            saveTaskTypes();
            saveTaskTypeFilters(oldType, newType);
            showStatus(t('types.renamed', 'Task-Typ "{old}" wurde in "{new}" umbenannt.', { old: oldType, new: newType }));
        } else {
            showStatus(t('admin.partialTaskTypeRename', 'Einige Tasks konnten nicht umbenannt werden. Beide Typen bleiben erhalten.'), true);
        }
        renderTaskTypes();
    } catch (error) {
        console.error('Task-Typ konnte nicht umbenannt werden:', error);
        showStatus(error.message, true);
        taskTypes = loadTaskTypes();
        renderTaskTypes();
    }
}

function updateTaskTypeColor(type, color) {
    try {
        taskTypes[type].color = color;
        saveTaskTypes();
        showStatus(t('admin.taskTypeSaved', 'Task-Typ gespeichert.'));
    } catch (error) {
        console.error('Task-Typ-Farbe konnte nicht gespeichert werden:', error);
        showStatus(error.message, true);
        taskTypes = loadTaskTypes();
        renderTaskTypes();
    }
}

async function deleteTaskType(type) {
    if (Object.keys(taskTypes).length <= 1) {
        showStatus(t('types.oneRequired', 'Mindestens ein Task-Typ muss auf dem Board erhalten bleiben!'), true);
        return;
    }
    try {
        const tasks = (await getAllTasks()).filter(task => task.taskType === type);
        if (!window.confirm(t('admin.confirmDeleteTaskType', 'Task-Typ "{type}" und {count} zugehörige Tasks löschen?', {
            type,
            count: tasks.length
        }))) return;
        const results = await Promise.allSettled(tasks.map(task =>
            requestTask(`/${encodeURIComponent(task.id)}`, { method: 'DELETE' })
        ));
        if (results.some(result => result.status === 'rejected')) {
            showStatus(t('admin.partialTaskTypeDelete', 'Einige Tasks konnten nicht gelöscht werden. Der Task-Typ bleibt erhalten.'), true);
            return;
        }
        const deletedConfig = taskTypes[type];
        delete taskTypes[type];
        try {
            saveTaskTypes();
        } catch (error) {
            taskTypes[type] = deletedConfig;
            throw error;
        }
        saveTaskTypeFilters(null, null, type);
        renderTaskTypes();
        showStatus(t('admin.taskTypeDeleted', 'Task-Typ und zugehörige Tasks gelöscht.'));
    } catch (error) {
        console.error('Task-Typ konnte nicht gelöscht werden:', error);
        showStatus(error.message, true);
    }
}

document.getElementById('task-type-form').addEventListener('submit', event => {
    event.preventDefault();
    const nameInput = document.getElementById('new-type-name');
    const name = nameInput.value.trim();
    if (!name) return;
    if (Object.prototype.hasOwnProperty.call(taskTypes, name)) {
        showStatus(t('types.duplicateName', 'Typ-Name existiert bereits.'), true);
        return;
    }

    const path = ['ToDo'];
    if (document.getElementById('path-analysis').checked) path.push('Analysis');
    if (document.getElementById('path-inprogress').checked) path.push('InProgress');
    path.push('Done');
    taskTypes[name] = { path, color: document.getElementById('new-type-color').value };
    try {
        saveTaskTypes();
        nameInput.value = '';
        renderTaskTypes();
        showStatus(t('admin.taskTypeSaved', 'Task-Typ gespeichert.'));
    } catch (error) {
        console.error('Task-Typ konnte nicht gespeichert werden:', error);
        delete taskTypes[name];
        showStatus(error.message, true);
    }
});

function editUser(user = null) {
    if (userPanel.classList.contains('hidden'))
        panelOpener = document.activeElement;
    showStatus('');
    form.reset();
    form.elements.id.value = user?.id || '';
    form.elements.name.value = user?.name || '';
    form.elements.email.value = user?.email || '';
    form.elements.role.value = user?.role || 'User';
    form.elements.active.checked = user?.active ?? true;
    form.elements.password.required = !user;
    document.getElementById('password-label').classList.toggle('hidden', !!user);
    document.getElementById('active-label').classList.toggle('hidden', !user);
    document.getElementById('form-title').textContent = user ? t('admin.editUser', 'Benutzer bearbeiten') : t('admin.newUser', 'Neuer Benutzer');
    userPanel.classList.remove('hidden');
    userPanel.classList.add('flex');
    panelResizer.classList.remove('hidden');
    form.elements.name.focus();
}

async function showView(view) {
    showStatus(t('admin.loading', 'Informationen werden geladen …'));
    try {
        if (view === 'users') {
            users = await request('users');
            renderUsers();
        } else if (view === 'translations') {
            translations = await request('translations');
            renderTranslations();
        } else if (view === 'types') {
            await request('users');
            taskTypes = loadTaskTypes();
            renderTaskTypes();
        } else if (view === 'tags') {
            await request('users');
            tagsConfig = loadTags();
            renderTags();
        } else {
            const database = await request('database');
            document.getElementById('provider').textContent = database.provider;
            document.getElementById('location').textContent = database.location;
        }
        document.querySelectorAll('[data-view]').forEach(button => {
            const selected = button.dataset.view === view;
            button.classList.toggle('bg-blue-50', selected);
            button.classList.toggle('text-blue-700', selected);
            button.setAttribute('aria-current', selected ? 'page' : 'false');
        });
        document.getElementById('users-view').classList.toggle('hidden', view !== 'users');
        document.getElementById('types-view').classList.toggle('hidden', view !== 'types');
        document.getElementById('tags-view').classList.toggle('hidden', view !== 'tags');
        document.getElementById('database-view').classList.toggle('hidden', view !== 'database');
        document.getElementById('translations-view').classList.toggle('hidden', view !== 'translations');
        activeView = view;
        closeUserPanel();
        showStatus('');
        return true;
    } catch (error) {
        console.error('Administration konnte nicht geladen werden:', error);
        document.getElementById('users-view').classList.add('hidden');
        document.getElementById('types-view').classList.add('hidden');
        document.getElementById('tags-view').classList.add('hidden');
        document.getElementById('database-view').classList.add('hidden');
        document.getElementById('translations-view').classList.add('hidden');
        closeUserPanel();
        showStatus(error.message, true);
        return false;
    }
}

form.addEventListener('submit', async event => {
    event.preventDefault();
    const id = form.elements.id.value;
    const body = {
        name: form.elements.name.value.trim(),
        email: form.elements.email.value.trim(),
        role: form.elements.role.value
    };
    if (id) {
        body.active = form.elements.active.checked;
        if (!body.active && !window.confirm(t('admin.confirmDeactivate', 'Konto deaktivieren? Die zugehörigen Tasks bleiben erhalten.'))) return;
    } else {
        body.password = form.elements.password.value;
    }
    const submit = document.getElementById('save-user');
    submit.disabled = true;
    try {
        await request(id ? `users/${encodeURIComponent(id)}` : 'users', {
            method: id ? 'PUT' : 'POST',
            body: JSON.stringify(body)
        });
        if (await showView('users'))
            showStatus(t('admin.userSaved', 'Benutzer gespeichert.'));
    } catch (error) {
        console.error('Benutzer konnte nicht gespeichert werden:', error);
        showStatus(error.message, true);
    } finally {
        submit.disabled = false;
    }
});

document.querySelectorAll('[data-view]').forEach(button => button.addEventListener('click', () => showView(button.dataset.view)));
document.getElementById('user-search').addEventListener('input', renderUsers);
document.getElementById('translation-search').addEventListener('input', renderTranslations);
window.addEventListener('languagechange', () => {
    if (activeView === 'types') renderTaskTypes();
    if (activeView === 'tags') renderTags();
});
window.addEventListener('beforeunload', event => {
    if (translationDrafts.size) {
        event.preventDefault();
        event.returnValue = '';
    }
});
document.getElementById('translation-form').addEventListener('submit', async event => {
    event.preventDefault();
    const target = event.currentTarget;
    const key = target.elements.key.value.trim();
    const body = {
        category: target.elements.category.value.trim(),
        german: target.elements.german.value.trim(),
        english: target.elements.english.value.trim()
    };
    const submit = target.querySelector('[type=submit]');
    submit.disabled = true;
    try {
        await request(`translations/${encodeURIComponent(key)}`, { method: 'POST', body: JSON.stringify(body) });
        target.reset();
        if (await showView('translations')) showStatus(t('admin.keyAdded', 'Schlüssel hinzugefügt.'));
    } catch (error) {
        console.error('Schlüssel konnte nicht hinzugefügt werden:', error);
        showStatus(error.message, true);
    } finally { submit.disabled = false; }
});
document.getElementById('new-user').addEventListener('click', () => editUser());
document.getElementById('cancel-user').addEventListener('click', closeUserPanel);
document.getElementById('close-user-panel').addEventListener('click', closeUserPanel);
document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && !userPanel.classList.contains('hidden'))
        closeUserPanel();
});

function setPanelWidth(width) {
    const limited = Math.max(350, Math.min(800, width));
    userPanel.style.width = `${limited}px`;
    panelResizer.setAttribute('aria-valuenow', limited);
}

panelResizer.addEventListener('mousedown', event => {
    event.preventDefault();
    document.body.classList.add('is-resizing');
    const resize = moveEvent => setPanelWidth(window.innerWidth - moveEvent.clientX);
    const stop = () => {
        document.body.classList.remove('is-resizing');
        document.removeEventListener('mousemove', resize);
        document.removeEventListener('mouseup', stop);
    };
    document.addEventListener('mousemove', resize);
    document.addEventListener('mouseup', stop);
});
panelResizer.addEventListener('keydown', event => {
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    event.preventDefault();
    setPanelWidth(parseInt(userPanel.style.width, 10) + (event.key === 'ArrowLeft' ? 20 : -20));
});
async function initializeAdmin() {
    await window.i18n.ready;
    if (!await showView('users')) return;
    try {
        await request('translations');
        document.querySelector('[data-view="translations"]').classList.remove('hidden');
    } catch (error) {
        console.warn('Übersetzungsverwaltung ist auf diesem Server nicht verfügbar:', error);
        if (!(error instanceof FeatureUnavailableError))
            showStatus(`Übersetzungsverwaltung konnte nicht geladen werden: ${error.message}`, true);
    }
}
initializeAdmin();
