'use strict';

(() => {
    const tabs = [...document.querySelectorAll('#view-tabs [role="tab"]')];
    const panels = {
        list: document.getElementById('hierarchy-panel'),
        kanban: document.getElementById('kanban-panel'),
        dashboard: document.getElementById('dashboard-panel')
    };
    const boardControls = ['board-header-actions', 'board-filters', 'board-groupings'].map(id => document.getElementById(id));
    const taskPanel = document.getElementById('task-details-panel');
    const detailsResizer = document.getElementById('details-resizer');
    const hierarchyTaskSlot = document.getElementById('hierarchy-task-panel-slot');
    const hierarchyEntityEditor = document.getElementById('hierarchy-entity-editor');
    let active = null;
    let currentUserId;

    function placeTaskPanel(view) {
        if (view === 'list') {
            if (taskPanel.parentElement !== hierarchyTaskSlot) {
                taskPanel.dataset.kanbanWidth = taskPanel.style.width || '450px';
                hierarchyTaskSlot.append(taskPanel);
            }
            taskPanel.style.width = '100%';
            const isOpen = !taskPanel.classList.contains('hidden');
            hierarchyTaskSlot.classList.toggle('hidden', !isOpen);
            hierarchyEntityEditor.classList.toggle('hidden', isOpen);
            detailsResizer.classList.add('hidden');
            return;
        }

        if (taskPanel.parentElement !== panels.kanban) {
            panels.kanban.append(taskPanel);
            taskPanel.style.width = taskPanel.dataset.kanbanWidth || localStorage.getItem('kanban_panel_width') || '450px';
        }
        hierarchyTaskSlot.classList.add('hidden');
        hierarchyEntityEditor.classList.remove('hidden');
        detailsResizer.classList.toggle('hidden', taskPanel.classList.contains('hidden'));
    }

    function show(view, updateUrl = true) {
        if (view === 'projects') view = 'list';
        if (!Object.hasOwn(panels, view) || view === active) return false;
        if (active === 'list' && !window.hierarchyView.leave()) return false;
        placeTaskPanel(view);
        active = view;
        const isBoardView = view === 'kanban' || view === 'list';
        for (const tab of tabs) {
            const selected = tab.dataset.view === view;
            tab.setAttribute('aria-selected', String(selected));
            tab.tabIndex = selected || (!isBoardView && tab.dataset.view === 'kanban') ? 0 : -1;
        }
        for (const [name, panel] of Object.entries(panels)) {
            const selected = name === view;
            panel.classList.toggle('hidden', !selected);
            if (name === 'kanban') panel.classList.toggle('flex', selected);
        }
        for (const control of boardControls) {
            control.classList.toggle('hidden', !isBoardView);
            control.classList.toggle('flex', isBoardView);
        }
        document.body.classList.toggle('details-panel-open', isBoardView && !document.getElementById('task-details-panel').classList.contains('hidden'));
        if (view === 'list') window.hierarchyView.activate();
        window.dispatchEvent(new CustomEvent('board-view-changed', { detail: view }));
        if (updateUrl) {
            const url = new URL(window.location.href);
            if (view === 'kanban') url.searchParams.delete('view');
            else url.searchParams.set('view', view);
            window.history.replaceState(window.history.state, '', url);
        }
        return true;
    }

    for (const tab of tabs) {
        tab.addEventListener('click', () => show(tab.dataset.view));
        tab.addEventListener('keydown', event => {
            if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
            event.preventDefault();
            const index = tabs.indexOf(tab);
            const target = event.key === 'Home' ? 0 : event.key === 'End' ? tabs.length - 1 :
                (index + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
            if (show(tabs[target].dataset.view)) tabs[target].focus();
        });
    }
    document.getElementById('open-dashboard').addEventListener('click', () => show('dashboard'));
    window.addEventListener('app-auth-changed', event => {
        const id = event.detail?.userId || event.detail?.id || event.detail?.email || null;
        if (id === currentUserId) return;
        currentUserId = id;
        if (active === 'list') {
            window.hierarchyView.reset();
            window.hierarchyView.activate();
        }
    });
    window.viewTabs = { current: () => active, show };
    const requested = new URLSearchParams(window.location.search).get('view');
    show(requested === 'projects' ? 'list' : requested && Object.hasOwn(panels, requested) ? requested : 'kanban', false);
})();
