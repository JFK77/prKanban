'use strict';

const statusMessage = document.getElementById('status');
const form = document.getElementById('user-form');
const userList = document.getElementById('user-list');
const userPanel = document.getElementById('user-panel');
const panelResizer = document.getElementById('user-panel-resizer');
const panelStatus = document.getElementById('user-panel-status');
let users = [];
let panelOpener = null;

async function request(path, options = {}) {
    const response = await fetch(`/api/v1/admin/${path}`, {
        credentials: 'same-origin',
        ...options,
        headers: options.body ? { 'Content-Type': 'application/json' } : {}
    });
    if (response.redirected || response.status === 401)
        throw new Error('Bitte als Administrator anmelden.');
    if (response.status === 403)
        throw new Error('Nur Administratoren können diesen Bereich öffnen.');
    if (!response.ok) {
        const contentType = response.headers.get('content-type') || '';
        const error = contentType.includes('json') ? await response.json() : null;
        const validation = error?.errors ? Object.values(error.errors).flat().join(' ') : '';
        throw new Error(error?.detail || validation || `Anfrage fehlgeschlagen (HTTP ${response.status}).`);
    }
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
        active.textContent = user.active ? 'Aktiv' : 'Inaktiv';
        const action = document.createElement('td');
        action.className = 'p-4';
        const edit = document.createElement('button');
        edit.type = 'button';
        edit.className = 'text-blue-700 font-semibold hover:underline';
        edit.textContent = 'Bearbeiten';
        edit.addEventListener('click', () => editUser(user));
        action.append(edit);
        row.append(identity, role, active, action);
        userList.append(row);
    }
}

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
    document.getElementById('form-title').textContent = user ? 'Benutzer bearbeiten' : 'Neuer Benutzer';
    userPanel.classList.remove('hidden');
    userPanel.classList.add('flex');
    panelResizer.classList.remove('hidden');
    form.elements.name.focus();
}

async function showView(view) {
    showStatus('Informationen werden geladen …');
    try {
        if (view === 'users') {
            users = await request('users');
            renderUsers();
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
        document.getElementById('database-view').classList.toggle('hidden', view !== 'database');
        closeUserPanel();
        showStatus('');
        return true;
    } catch (error) {
        console.error('Administration konnte nicht geladen werden:', error);
        document.getElementById('users-view').classList.add('hidden');
        document.getElementById('database-view').classList.add('hidden');
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
        if (!body.active && !window.confirm('Konto deaktivieren? Die zugehörigen Tasks bleiben erhalten.')) return;
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
            showStatus('Benutzer gespeichert.');
    } catch (error) {
        console.error('Benutzer konnte nicht gespeichert werden:', error);
        showStatus(error.message, true);
    } finally {
        submit.disabled = false;
    }
});

document.querySelectorAll('[data-view]').forEach(button => button.addEventListener('click', () => showView(button.dataset.view)));
document.getElementById('user-search').addEventListener('input', renderUsers);
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
showView('users');
