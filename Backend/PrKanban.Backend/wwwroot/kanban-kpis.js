const DAY = 24 * 60 * 60 * 1000;
const COLUMNS = ['Backlog', 'ToDo', 'Analysis', 'InProgress', 'Done'];
const COLORS = ['#94a3b8', '#60a5fa', '#a78bfa', '#f59e0b', '#34d399'];

function utcTime(value) {
    if (!value) return NaN;
    return Date.parse(/[zZ]|[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`);
}

export function calculateKanbanKpis(tasks, now = Date.now()) {
    const today = Math.floor(now / DAY) * DAY;
    const start = today - 29 * DAY;
    const completions = [];
    const aging = [];
    const daily = Array.from({ length: 30 }, (_, index) => ({
        date: start + index * DAY,
        counts: Object.fromEntries(COLUMNS.map(column => [column, 0]))
    }));

    for (const task of tasks) {
        const events = (task.statusHistory || [])
            .map(event => ({ ...event, time: utcTime(event.occurredAt) }))
            .filter(event => Number.isFinite(event.time) && event.time <= now)
            .sort((a, b) => a.time - b.time || a.id - b.id);
        const created = utcTime(task.createdAt);
        let previous = null;
        let started = null;
        let columnEntered = null;
        let columnEntryKnown = false;
        for (const event of events) {
            if (event.columnName !== previous) {
                columnEntered = event.time;
                columnEntryKnown = !event.isBaseline;
                if (event.columnName === 'InProgress' && !event.isBaseline && started === null)
                    started = event.time;
                if (event.columnName === 'Done' && previous !== null && previous !== 'Done') {
                    if (event.time >= start) {
                        completions.push({
                            task,
                            time: event.time,
                            cycle: started !== null && started <= event.time ? (event.time - started) / DAY : null,
                            lead: Number.isFinite(created) && created <= event.time ? (event.time - created) / DAY : null
                        });
                    }
                    started = null;
                }
                previous = event.columnName;
            }
        }
        if (previous && previous !== 'Backlog' && previous !== 'Done') {
            aging.push({
                task, column: previous,
                days: columnEntryKnown && columnEntered !== null ? (now - columnEntered) / DAY : null
            });
        }
        let eventIndex = 0;
        let column = null;
        for (const day of daily) {
            const end = Math.min(now, day.date + DAY - 1);
            while (eventIndex < events.length && events[eventIndex].time <= end)
                column = events[eventIndex++].columnName;
            if (Object.hasOwn(day.counts, column)) day.counts[column]++;
        }
    }

    completions.sort((a, b) => a.time - b.time || a.task.id.localeCompare(b.task.id));
    aging.sort((a, b) => (b.days ?? -1) - (a.days ?? -1) || a.task.id.localeCompare(b.task.id));
    const average = values => values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
    return {
        start, daily, completions, aging,
        cycle: average(completions.map(item => item.cycle).filter(value => value !== null)),
        lead: average(completions.map(item => item.lead).filter(value => value !== null)),
        throughput: completions.length
    };
}

function svgElement(name, attributes = {}) {
    const element = document.createElementNS('http://www.w3.org/2000/svg', name);
    for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, String(value));
    return element;
}

function addText(svg, x, y, text, attributes = {}) {
    const node = svgElement('text', { x, y, fill: 'currentColor', 'font-size': 10, ...attributes });
    node.textContent = text;
    svg.append(node);
}

function addTitle(node, text) {
    const title = svgElement('title');
    title.textContent = text;
    node.append(title);
}

function chartFrame(label, interactive = false, axes = null) {
    const svg = svgElement('svg', {
        viewBox: `0 0 360 ${axes ? 214 : 200}`, role: interactive ? 'group' : 'img', 'aria-label': label,
        class: 'mt-3 w-full text-slate-600 dark:text-slate-300'
    });
    svg.append(svgElement('path', { d: 'M 35 12 V 170 H 350', fill: 'none', stroke: 'currentColor', 'stroke-width': 1 }));
    if (axes) {
        addText(svg, 192, 206, axes.x, { 'text-anchor': 'middle', 'font-weight': 600 });
        addText(svg, 0, 0, axes.y, { 'text-anchor': 'middle', 'font-weight': 600, transform: 'translate(10 91) rotate(-90)' });
    }
    return svg;
}

export function renderEmptyFlowChart(t) {
    const note = document.createElement('p');
    note.className = 'mt-3 flex min-h-48 items-center justify-center px-4 text-center text-base text-slate-400 dark:text-slate-500';
    note.textContent = t('kpi.noDataForPeriod', 'Für diesen Zeitraum sind noch keine Daten verfügbar. Bitte überprüfen Sie auch die Filter.');
    return note;
}

export function renderTaskDistribution(tasks, t, getColumnLabel) {
    const panel = document.createElement('div');
    panel.className = 'min-w-0 overflow-x-auto rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900';
    if (!tasks.length) {
        panel.append(renderEmptyFlowChart(t));
        return panel;
    }

    const counts = new Map();
    tasks.forEach(task => counts.set(task.column, (counts.get(task.column) || 0) + 1));
    const columns = [...COLUMNS, ...[...counts.keys()].filter(column => !COLUMNS.includes(column)).sort()];
    const max = Math.max(...counts.values());
    const chart = document.createElement('div');
    chart.className = 'flex min-w-full gap-2';
    for (const column of columns) {
        const count = counts.get(column) || 0;
        const item = document.createElement('div');
        item.className = 'flex min-w-[3rem] flex-1 flex-col items-center';
        const plot = document.createElement('div');
        plot.className = 'flex h-32 w-full flex-col items-center justify-end';
        const value = document.createElement('span');
        value.className = 'mb-1 text-xs tabular-nums text-slate-700 dark:text-slate-200';
        value.textContent = String(count);
        const bar = document.createElement('div');
        bar.className = 'w-3/4 max-w-8 rounded-t bg-blue-500';
        bar.style.height = `${count / max * 104}px`;
        bar.setAttribute('aria-hidden', 'true');
        const label = document.createElement('span');
        label.className = 'mt-2 w-full break-words text-center text-xs text-slate-700 dark:text-slate-200';
        label.textContent = getColumnLabel(column);
        plot.append(value, bar);
        item.append(plot, label);
        chart.append(item);
    }
    panel.append(chart);
    return panel;
}

export function renderFlowChart(kind, data, t, getColumnLabel, openTask) {
    const wrapper = document.createElement('div');
    const empty = () => {
        wrapper.append(renderEmptyFlowChart(t));
        return wrapper;
    };
    const date = time => new Date(time).toISOString().slice(5, 10);
    const daysLabel = t('kpi.days', 'Tage');
    if (kind === 'cfd') {
        const max = Math.max(0, ...data.daily.map(day => Object.values(day.counts).reduce((a, b) => a + b, 0)));
        if (!max) return empty();
        const svg = chartFrame(t('kpi.cfd', 'Cumulative Flow Diagram'), false,
            { x: t('kpi.axisDate', 'Datum'), y: t('kpi.axisTasks', 'Aufgaben') });
        const x = index => 35 + index * 315 / (data.daily.length - 1);
        const y = value => 170 - value / max * 150;
        let lower = data.daily.map(() => 0);
        COLUMNS.forEach((column, index) => {
            const upper = lower.map((value, i) => value + data.daily[i].counts[column]);
            const points = upper.map((value, i) => `${x(i)},${y(value)}`)
                .concat(lower.map((value, i) => `${x(i)},${y(value)}`).reverse()).join(' ');
            const area = svgElement('polygon', { points, fill: COLORS[index], opacity: 0.8 });
            addTitle(area, getColumnLabel(column));
            svg.append(area);
            lower = upper;
        });
        addText(svg, 30, 20, String(max), { 'text-anchor': 'end' });
        addText(svg, 35, 188, date(data.daily[0].date));
        addText(svg, 350, 188, date(data.daily.at(-1).date), { 'text-anchor': 'end' });
        wrapper.append(svg);
        const legend = document.createElement('div');
        legend.className = 'flex flex-wrap gap-2 text-xs text-slate-600 dark:text-slate-300';
        COLUMNS.forEach((column, index) => {
            const item = document.createElement('span');
            const swatch = document.createElement('span');
            swatch.className = 'mr-1 inline-block h-2 w-2';
            swatch.style.backgroundColor = COLORS[index];
            item.append(swatch, document.createTextNode(getColumnLabel(column)));
            legend.append(item);
        });
        wrapper.append(legend);
        return wrapper;
    }
    if (kind === 'aging') {
        const items = data.aging.filter(item => item.days !== null);
        if (!items.length) return empty();
        const list = document.createElement('div');
        list.className = 'mt-3 space-y-2';
        const max = Math.max(1, ...items.map(item => item.days));
        for (const item of items) {
            const row = document.createElement('button');
            row.type = 'button';
            row.className = 'w-full cursor-pointer rounded text-left text-xs text-slate-600 hover:text-blue-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-500 dark:text-slate-300 dark:hover:text-blue-400';
            row.setAttribute('aria-label', t('kpi.openTask', 'Task {task} im Kanban öffnen', {
                task: item.task.taskNumber || item.task.title
            }));
            row.addEventListener('click', () => openTask(item.task.id));
            const label = document.createElement('div');
            label.className = 'flex justify-between gap-2';
            const name = document.createElement('span');
            name.textContent = `${item.task.taskNumber || item.task.title} · ${getColumnLabel(item.column)}`;
            const value = document.createElement('span');
            value.textContent = `${item.days.toFixed(1)} ${daysLabel}`;
            const track = document.createElement('div');
            track.className = 'mt-1 h-2 rounded-full bg-slate-100 dark:bg-slate-800';
            const bar = document.createElement('div');
            bar.className = 'h-full rounded-full bg-amber-500';
            bar.style.width = `${Math.max(1, item.days / max * 100)}%`;
            track.append(bar);
            label.append(name, value);
            row.append(label, track);
            list.append(row);
        }
        wrapper.append(list);
        if (data.aging.some(item => item.days === null)) {
            const note = document.createElement('p');
            note.className = 'mt-2 text-xs text-slate-500 dark:text-slate-400';
            note.textContent = t('kpi.unknownAges', 'Aufgaben ohne bekannten Spalteneintritt sind nicht dargestellt.');
            wrapper.append(note);
        }
        return wrapper;
    }
    const items = data.completions.filter(item => item.cycle !== null);
    if (!items.length) return empty();
    const svg = chartFrame(kind === 'control' ? t('kpi.controlChart', 'Control Chart') : t('kpi.scatterplot', 'Cycle Time Scatterplot'), true, {
        x: kind === 'control' ? t('kpi.axisCompletions', 'Abschlüsse (Reihenfolge)') : t('kpi.axisDate', 'Datum'),
        y: t('kpi.axisCycleTime', 'Cycle Time (Tage)')
    });
    const max = Math.max(1, ...items.map(item => item.cycle));
    const x = (item, index) => kind === 'control'
        ? 35 + (items.length === 1 ? 157.5 : index * 315 / (items.length - 1))
        : 35 + (item.time - data.start) / (30 * DAY) * 315;
    const y = value => 170 - value / max * 150;
    if (kind === 'scatter') {
        const sorted = items.map(item => item.cycle).sort((a, b) => a - b);
        const percentile = sorted[Math.ceil(sorted.length * 0.85) - 1];
        svg.append(svgElement('line', { x1: 35, y1: y(percentile), x2: 350, y2: y(percentile), stroke: '#ef4444', 'stroke-dasharray': '4 3' }));
        addText(svg, 348, Math.max(10, y(percentile) - 3), `P85: ${percentile.toFixed(1)} ${daysLabel}`, { 'text-anchor': 'end' });
    }
    const points = new Map();
    items.forEach((item, index) => {
        const cx = x(item, index);
        const cy = y(item.cycle);
        const key = `${cx}:${cy}`;
        if (!points.has(key)) points.set(key, { cx, cy, items: [] });
        points.get(key).items.push(item);
    });
    const choices = document.createElement('div');
    choices.className = 'mt-2 flex flex-col gap-1 text-xs';
    points.forEach(({ cx, cy, items: pointItems }) => {
        const labelFor = item => `${item.task.taskNumber || item.task.title}: ${item.cycle.toFixed(1)} ${daysLabel} (${date(item.time)})`;
        const label = labelFor(pointItems[0]);
        const accessibleLabel = pointItems.length === 1
            ? t('kpi.openTask', 'Task {task} im Kanban öffnen', { task: label })
            : t('kpi.overlappingTasks', '{count} Aufgaben an diesem Punkt. Aufgabe auswählen.', { count: pointItems.length });
        const point = svgElement('circle', {
            cx, cy, r: 4, fill: '#3b82f6',
            role: 'button', tabindex: 0, class: 'cursor-pointer focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-500',
            'aria-label': accessibleLabel
        });
        addTitle(point, pointItems.map(labelFor).join('\n'));
        const activate = () => {
            if (pointItems.length === 1) {
                openTask(pointItems[0].task.id);
                return;
            }
            choices.replaceChildren();
            const heading = document.createElement('p');
            heading.textContent = t('kpi.chooseTask', 'Aufgabe auswählen:');
            choices.append(heading);
            for (const item of pointItems) {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'rounded px-2 py-1 text-left text-blue-600 hover:bg-slate-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-500 dark:text-blue-400 dark:hover:bg-slate-800';
                button.textContent = labelFor(item);
                button.addEventListener('click', () => openTask(item.task.id));
                choices.append(button);
            }
            choices.querySelector('button').focus();
        };
        point.addEventListener('click', activate);
        point.addEventListener('keydown', event => {
            if (event.key !== 'Enter' && event.key !== ' ') return;
            event.preventDefault();
            activate();
        });
        svg.append(point);
    });
    addText(svg, 30, 20, max.toFixed(1), { 'text-anchor': 'end' });
    addText(svg, 35, 188, kind === 'control' ? '1' : date(data.start));
    addText(svg, 350, 188, kind === 'control' ? String(items.length) : date(data.daily.at(-1).date), { 'text-anchor': 'end' });
    wrapper.append(svg, choices);
    return wrapper;
}
