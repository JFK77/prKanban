'use strict';

(() => {
    let catalog = new Map();
    let language = 'de';
    let user = null;
    let requestInProgress = false;
    let userLoaded = false;

    function text(key, fallback, values = {}) {
        const entry = catalog.get(key);
        const template = entry?.[language === 'en' ? 'english' : 'german'] || fallback;
        return template.replace(/\{([A-Za-z][A-Za-z0-9]*)\}/g,
            (match, name) => Object.prototype.hasOwnProperty.call(values, name) ? String(values[name]) : match);
    }

    function apply() {
        document.documentElement.lang = language;
        document.documentElement.style.setProperty('--board-empty-label', JSON.stringify(text('board.noTasks', 'Keine Aufgaben')));
        document.querySelectorAll('[data-i18n]').forEach(element => {
            const fallback = element.dataset.i18nFallback ?? element.textContent;
            element.dataset.i18nFallback = fallback;
            element.textContent = text(element.dataset.i18n, fallback);
        });
        for (const attribute of ['title', 'placeholder', 'aria-label']) {
            const dataKey = `i18n${attribute.split('-').map(part => part[0].toUpperCase() + part.slice(1)).join('')}`;
            document.querySelectorAll(`[data-i18n-${attribute}]`).forEach(element => {
                const fallbackKey = `${dataKey}Fallback`;
                const fallback = element.dataset[fallbackKey] ?? element.getAttribute(attribute) ?? '';
                element.dataset[fallbackKey] = fallback;
                element.setAttribute(attribute, text(element.dataset[dataKey], fallback));
            });
        }
        const selector = document.getElementById('language-selector');
        if (selector) selector.value = language;
        window.dispatchEvent(new Event('languagechange'));
    }

    async function getJson(url, options = {}) {
        const response = await fetch(url, { credentials: 'same-origin', ...options });
        if (!response.ok || response.redirected || !(response.headers.get('content-type') || '').includes('json'))
            throw new Error(`HTTP ${response.status}`);
        return response.json();
    }

    async function refresh() {
        const entries = await getJson('/api/v1/translations');
        if (!Array.isArray(entries)) throw new Error('Invalid translation catalogue');
        catalog = new Map(entries.map(entry => [entry.key, entry]));
        apply();
    }

    function setUser(nextUser) {
        user = nextUser;
        userLoaded = true;
        language = nextUser?.language === 'en' ? 'en' : 'de';
        apply();
    }

    async function setLanguage(nextLanguage) {
        if (!['de', 'en'].includes(nextLanguage) || requestInProgress) return;
        requestInProgress = true;
        const selector = document.getElementById('language-selector');
        if (selector) {
            selector.disabled = true;
            selector.dispatchEvent(new Event('language-selector-update'));
        }
        try {
            if (!userLoaded) await loadUser();
            if (catalog.size === 0) await refresh();
            if (user) {
                await getJson('/api/v1/auth/language', {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ language: nextLanguage })
                });
                user.language = nextLanguage;
            }
            language = nextLanguage;
            apply();
        } finally {
            requestInProgress = false;
            if (selector) {
                selector.disabled = false;
                selector.dispatchEvent(new Event('language-selector-update'));
            }
        }
    }

    function report(error) {
        console.error('Translation system error:', error);
        const notice = document.getElementById('language-error');
        if (notice) {
            notice.textContent = language === 'en'
                ? 'Language settings could not be loaded or saved. Please try again.'
                : 'Die Spracheinstellungen konnten nicht geladen oder gespeichert werden. Bitte erneut versuchen.';
            notice.hidden = false;
        }
    }

    async function loadUser() {
        const response = await fetch('/api/v1/auth/user', { credentials: 'same-origin' });
        if (response.ok && !response.redirected) {
            const result = await response.json();
            user = result.data;
            language = user?.language === 'en' ? 'en' : 'de';
        } else if (response.status !== 401 && !response.redirected) {
            throw new Error(`HTTP ${response.status}`);
        }
        userLoaded = true;
    }

    const ready = (async () => {
        await loadUser();
        await refresh();
    })().catch(report);

    document.addEventListener('DOMContentLoaded', () => {
        const selector = document.getElementById('language-selector');
        selector?.addEventListener('change', async () => {
            const notice = document.getElementById('language-error');
            if (notice) notice.hidden = true;
            try {
                await setLanguage(selector.value);
            } catch (error) {
                selector.value = language;
                selector.dispatchEvent(new Event('language-selector-update'));
                report(error);
            }
        });
    });

    window.i18n = { text, refresh, setUser, ready };
})();
