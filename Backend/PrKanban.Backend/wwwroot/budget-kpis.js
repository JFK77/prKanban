// Budget KPIs dashboard tab: pure aggregation helpers + DOM rendering.
// Data sources: filtered tasks (global board filters), hierarchy budgets
// (deliverable level) and per-task time entries supplied by the caller.

const WEEK_COUNT = 10;

function toNumber(value) {
    return Number.isFinite(value) ? value : Number.isFinite(Number(value)) ? Number(value) : 0;
}

function utcTime(value) {
    if (!value) return NaN;
    return Date.parse(/[zZ]|[+-]\d\d:\d\d$/.test(String(value)) ? value : `${value}Z`);
}

// ISO-8601 week (UTC) for calendar-week buckets ("KW 47").
function isoWeek(time) {
    const date = new Date(time);
    const day = (date.getUTCDay() + 6) % 7;
    date.setUTCDate(date.getUTCDate() - day + 3);
    const year = date.getUTCFullYear();
    const firstThursday = new Date(Date.UTC(year, 0, 4));
    const firstDay = (firstThursday.getUTCDay() + 6) % 7;
    firstThursday.setUTCDate(firstThursday.getUTCDate() - firstDay + 3);
    const week = 1 + Math.round((date - firstThursday) / (7 * 86400000));
    return { year, week, key: `${year}-W${String(week).padStart(2, '0')}` };
}

function sumEntries(entries, billableOnly) {
    let billable = 0;
    let nonBillable = 0;
    for (const entry of entries || []) {
        const hours = toNumber(entry.durationHours);
        if (entry.isBillable) billable += hours;
        else nonBillable += hours;
    }
    return { billable, nonBillable, total: billableOnly ? billable : billable + nonBillable, all: billable + nonBillable };
}

function ratio(numerator, denominator) {
    return denominator > 0 ? numerator / denominator : null;
}

/**
 * Aggregates all budget KPI data for the active scope.
 * Budgets live on deliverables (and optionally tasks); estimates live on tasks.
 * "Project budget" in this app is the sum of its deliverable budgets, so the
 * unallocated figure compares deliverable budgets against task-level budgets.
 * All divisions are guarded; missing budgets/estimates yield null metrics.
 *
 * @param {{tasks: Array, deliverables: Array, entriesByTask: Map, billableOnly: boolean}} input
 */
export function aggregateBudgetKpis({ tasks, deliverables, entriesByTask, billableOnly = false }) {
    const deliverableById = new Map((deliverables || []).map(item => [item.id, item]));
    const scopedDeliverables = new Map();

    const taskRows = (tasks || []).map(task => {
        const entries = entriesByTask?.get(task.id) || [];
        const split = sumEntries(entries, billableOnly);
        if (task.deliverableId && deliverableById.has(task.deliverableId))
            scopedDeliverables.set(task.deliverableId, deliverableById.get(task.deliverableId));
        return {
            task,
            budget: toNumber(task.budgetHours),
            estimate: toNumber(task.estimatedHours),
            actual: split.total,
            billable: split.billable,
            nonBillable: split.nonBillable,
            allActual: split.all,
            entries
        };
    });

    const budget = [...scopedDeliverables.values()].reduce((sum, item) => sum + toNumber(item.budgetHours), 0);
    const taskBudget = taskRows.reduce((sum, row) => sum + row.budget, 0);
    const estimate = taskRows.reduce((sum, row) => sum + row.estimate, 0);
    const billableActual = taskRows.reduce((sum, row) => sum + row.billable, 0);
    const nonBillableActual = taskRows.reduce((sum, row) => sum + row.nonBillable, 0);
    const actual = billableOnly ? billableActual : billableActual + nonBillableActual;

    const totals = {
        budget, taskBudget, estimate, actual, billableActual, nonBillableActual,
        burn: ratio(actual, budget),
        remaining: budget > 0 ? budget - actual : null,
        unallocated: budget > 0 || taskBudget > 0 ? budget - taskBudget : null,
        billableShare: ratio(billableActual, billableActual + nonBillableActual)
    };

    // --- hierarchy breakdown: customer -> project -> deliverable -> task ---
    const customers = new Map();
    for (const row of taskRows) {
        const task = row.task;
        const customerKey = task.customer || '—';
        const projectKey = task.projectId || task.projectName || '—';
        const deliverableKey = task.deliverableId || '—';
        if (!customers.has(customerKey))
            customers.set(customerKey, { name: customerKey, projects: new Map(), budget: 0, estimate: 0, actual: 0, billable: 0, nonBillable: 0, deliverableIds: new Set() });
        const customer = customers.get(customerKey);
        if (!customer.projects.has(projectKey))
            customer.projects.set(projectKey, { name: task.projectName || projectKey, deliverables: new Map(), budget: 0, estimate: 0, actual: 0, billable: 0, nonBillable: 0, deliverableIds: new Set() });
        const project = customer.projects.get(projectKey);
        if (!project.deliverables.has(deliverableKey)) {
            const info = deliverableById.get(deliverableKey);
            project.deliverables.set(deliverableKey, {
                name: task.deliverableName || info?.name || deliverableKey,
                budget: toNumber(info?.budgetHours),
                estimate: 0, actual: 0, billable: 0, nonBillable: 0, tasks: []
            });
        }
        const deliverable = project.deliverables.get(deliverableKey);
        deliverable.tasks.push(row);
        deliverable.estimate += row.estimate;
        deliverable.actual += row.actual;
        deliverable.billable += row.billable;
        deliverable.nonBillable += row.nonBillable;
        for (const node of [project, customer]) {
            node.estimate += row.estimate;
            node.actual += row.actual;
            node.billable += row.billable;
            node.nonBillable += row.nonBillable;
            if (!node.deliverableIds.has(deliverableKey)) {
                node.deliverableIds.add(deliverableKey);
                node.budget += toNumber(deliverableById.get(deliverableKey)?.budgetHours);
            }
        }
    }

    // --- weekly burn-rate buckets (last WEEK_COUNT ISO weeks with data) ---
    const weekBuckets = new Map();
    const allEntries = [];
    for (const row of taskRows) {
        for (const entry of row.entries) {
            const time = utcTime(entry.startTime);
            if (!Number.isFinite(time)) continue;
            allEntries.push({ time, hours: toNumber(entry.durationHours), isBillable: !!entry.isBillable });
            const { key, week, year } = isoWeek(time);
            if (!weekBuckets.has(key)) weekBuckets.set(key, { key, week, year, billable: 0, nonBillable: 0 });
            const bucket = weekBuckets.get(key);
            if (entry.isBillable) bucket.billable += toNumber(entry.durationHours);
            else bucket.nonBillable += toNumber(entry.durationHours);
        }
    }
    const weekly = [...weekBuckets.values()].sort((a, b) => a.key.localeCompare(b.key)).slice(-WEEK_COUNT);

    // --- cumulative burn over time (day resolution) ---
    allEntries.sort((a, b) => a.time - b.time);
    const cumulative = [];
    let running = 0;
    for (const entry of allEntries) {
        if (billableOnly && !entry.isBillable) continue;
        running += entry.hours;
        const day = Math.floor(entry.time / 86400000) * 86400000;
        if (cumulative.length && cumulative[cumulative.length - 1].time === day)
            cumulative[cumulative.length - 1].value = running;
        else cumulative.push({ time: day, value: running });
    }

    // --- overrun / risk ranking: tasks by utilisation of budget (or estimate) ---
    const risk = taskRows
        .map(row => {
            const reference = row.budget > 0 ? row.budget : row.estimate;
            const utilization = ratio(row.actual, reference);
            return utilization === null ? null : {
                task: row.task,
                referenceHours: reference,
                referenceKind: row.budget > 0 ? 'budget' : 'estimate',
                actual: row.actual,
                utilization,
                overrun: Math.max(0, row.actual - reference)
            };
        })
        .filter(Boolean)
        .sort((a, b) => b.utilization - a.utilization)
        .slice(0, 6);

    // --- estimate vs. actual scatter ---
    const scatter = taskRows
        .filter(row => row.estimate > 0 && row.allActual > 0)
        .map(row => ({ task: row.task, estimate: row.estimate, actual: billableOnly ? row.billable : row.allActual }));

    return { totals, customers, weekly, cumulative, risk, scatter, billableOnly };
}

// ---------------------------------------------------------------------------
// Rendering (matches the Kanban KPIs card/section design)
// ---------------------------------------------------------------------------

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

function chartFrame(label, interactive = false) {
    const svg = svgElement('svg', {
        viewBox: '0 0 360 200', role: interactive ? 'group' : 'img', 'aria-label': label,
        class: 'mt-3 w-full text-slate-600 dark:text-slate-300'
    });
    svg.append(svgElement('path', { d: 'M 35 12 V 170 H 350', fill: 'none', stroke: 'currentColor', 'stroke-width': 1 }));
    return svg;
}

function emptyChart(t) {
    const note = document.createElement('p');
    note.className = 'mt-3 flex min-h-48 items-center justify-center px-4 text-center text-base text-slate-400 dark:text-slate-500';
    note.textContent = t('kpi.noDataForPeriod', 'Für diesen Zeitraum sind noch keine Daten verfügbar. Bitte überprüfen Sie auch die Filter.');
    return note;
}

function formatHours(value) {
    if (value === null || !Number.isFinite(value)) return '—';
    const rounded = Math.round(value * 10) / 10;
    return `${rounded.toLocaleString('de-DE', { maximumFractionDigits: 1 })}h`;
}

function formatPercent(value) {
    return value === null ? '—' : `${Math.round(value * 100)}%`;
}

function heroCard(title, value, valueClass, explanation, note) {
    const container = document.createElement('article');
    container.className = 'rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900';
    const header = document.createElement('div');
    header.className = 'flex items-start justify-between gap-3';
    const labels = document.createElement('div');
    labels.className = 'min-w-0 flex-1';
    const heading = document.createElement('h4');
    heading.className = 'text-sm font-semibold text-slate-700 dark:text-slate-200';
    heading.textContent = title;
    labels.append(heading);
    if (note) {
        const noteNode = document.createElement('p');
        noteNode.className = 'mt-2 text-xs text-slate-500 dark:text-slate-400';
        noteNode.textContent = note;
        labels.append(noteNode);
    }
    const number = document.createElement('p');
    number.className = `max-w-[50%] break-words text-right text-3xl font-bold ${valueClass || 'text-slate-900 dark:text-slate-100'}`;
    number.textContent = value;
    header.append(labels, number);
    const description = document.createElement('p');
    description.className = 'mt-2 text-xs text-slate-500 dark:text-slate-400 italic';
    description.textContent = explanation;
    container.append(header, description);
    return container;
}

function chartCard(title, explanation, note) {
    const container = document.createElement('article');
    container.className = 'rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900';
    const heading = document.createElement('h4');
    heading.className = 'text-sm font-semibold text-slate-700 dark:text-slate-200';
    heading.textContent = title;
    const description = document.createElement('p');
    description.className = 'mt-2 text-xs text-slate-500 dark:text-slate-400';
    description.textContent = explanation;
    container.append(heading, description);
    if (note) {
        const noteNode = document.createElement('p');
        noteNode.className = 'mt-2 text-xs text-slate-500 dark:text-slate-400';
        noteNode.textContent = note;
        container.append(noteNode);
    }
    return container;
}

function burnColor(burn) {
    if (burn === null) return 'text-slate-900 dark:text-slate-100';
    if (burn > 1) return 'text-red-600 dark:text-red-400';
    if (burn >= 0.8) return 'text-amber-600 dark:text-amber-400';
    return 'text-emerald-600 dark:text-emerald-400';
}

function breakdownRow(t, level, name, node, openTask, taskId) {
    const reference = node.budget > 0 ? node.budget : node.estimate;
    const max = Math.max(node.budget, node.estimate, node.actual);
    const pct = value => max > 0 ? Math.min(100, value / max * 100) : 0;
    const overrun = reference > 0 ? node.actual - reference : 0;

    const row = document.createElement(taskId && openTask ? 'button' : 'div');
    row.className = 'w-full rounded px-2 py-1.5 text-left text-xs text-slate-600 dark:text-slate-300' +
        (taskId && openTask ? ' cursor-pointer hover:text-blue-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-500 dark:hover:text-blue-400' : '');
    if (taskId && openTask) {
        row.type = 'button';
        row.setAttribute('aria-label', t('kpi.openTask', 'Task {task} im Kanban öffnen', { task: name }));
        row.addEventListener('click', () => openTask(taskId));
    }
    const head = document.createElement('div');
    head.className = 'flex flex-wrap items-center justify-between gap-2';
    const label = document.createElement('span');
    label.className = level === 0 ? 'font-semibold text-slate-800 dark:text-slate-100'
        : level === 1 ? 'font-medium text-slate-700 dark:text-slate-200' : '';
    label.textContent = name;
    const figures = document.createElement('span');
    figures.className = 'tabular-nums text-slate-500 dark:text-slate-400';
    figures.textContent = `${t('kpi.budgetShort', 'Budget')} ${formatHours(node.budget > 0 ? node.budget : null)} · ` +
        `${t('kpi.estimateShort', 'Schätzung')} ${formatHours(node.estimate > 0 ? node.estimate : null)} · ` +
        `${t('kpi.actualShort', 'Ist')} ${formatHours(node.actual)}`;
    head.append(label, figures);
    row.append(head);

    const bar = document.createElement('div');
    bar.className = 'mt-1 flex h-2 overflow-hidden rounded-full bg-slate-100 dark:bg-slate-800';
    const billablePart = document.createElement('div');
    billablePart.className = 'h-full bg-emerald-500';
    billablePart.style.width = `${pct(Math.min(node.billable, reference > 0 ? reference : node.billable))}%`;
    addNativeTitle(billablePart, `${t('kpi.billable', 'Abrechenbar')}: ${formatHours(node.billable)}`);
    const nonBillablePart = document.createElement('div');
    nonBillablePart.className = 'h-full bg-sky-500';
    nonBillablePart.style.width = `${pct(Math.max(0, Math.min(node.billable + node.nonBillable, reference > 0 ? reference : Infinity) - node.billable))}%`;
    addNativeTitle(nonBillablePart, `${t('kpi.nonBillable', 'Nicht abrechenbar')}: ${formatHours(node.nonBillable)}`);
    bar.append(billablePart, nonBillablePart);
    if (overrun > 0) {
        const overrunPart = document.createElement('div');
        overrunPart.className = 'h-full bg-red-500';
        overrunPart.style.width = `${pct(overrun)}%`;
        addNativeTitle(overrunPart, `${t('kpi.overrun', 'Überschreitung')}: +${formatHours(overrun)}`);
        bar.append(overrunPart);
        const badge = document.createElement('span');
        badge.className = 'ml-2 rounded bg-red-100 px-1.5 py-0.5 text-[10px] font-semibold text-red-700 dark:bg-red-900/40 dark:text-red-300';
        badge.textContent = `+${formatHours(overrun)} (${formatPercent(ratio(node.actual, reference))})`;
        head.append(badge);
    }
    row.append(bar);
    return row;
}

function addNativeTitle(node, text) {
    node.title = text;
}

function renderBreakdown(model, t, openTask) {
    const panel = document.createElement('div');
    panel.className = 'rounded-xl border border-slate-200 bg-white p-4 dark:border-slate-700 dark:bg-slate-900';
    if (!model.customers.size) {
        panel.append(emptyChart(t));
        return panel;
    }
    for (const customer of model.customers.values()) {
        const customerDetails = document.createElement('details');
        customerDetails.open = true;
        customerDetails.className = 'group';
        const customerSummary = document.createElement('summary');
        customerSummary.className = 'cursor-pointer list-none [&::-webkit-details-marker]:hidden';
        customerSummary.append(breakdownRow(t, 0, customer.name, customer));
        customerDetails.append(customerSummary);
        for (const project of customer.projects.values()) {
            const projectDetails = document.createElement('details');
            projectDetails.open = true;
            projectDetails.className = 'ml-4 border-l border-slate-200 pl-2 dark:border-slate-700';
            const projectSummary = document.createElement('summary');
            projectSummary.className = 'cursor-pointer list-none [&::-webkit-details-marker]:hidden';
            projectSummary.append(breakdownRow(t, 1, project.name, project));
            projectDetails.append(projectSummary);
            for (const deliverable of project.deliverables.values()) {
                const deliverableDetails = document.createElement('details');
                deliverableDetails.className = 'ml-4 border-l border-slate-200 pl-2 dark:border-slate-700';
                const deliverableSummary = document.createElement('summary');
                deliverableSummary.className = 'cursor-pointer list-none [&::-webkit-details-marker]:hidden';
                deliverableSummary.append(breakdownRow(t, 2, deliverable.name, deliverable));
                deliverableDetails.append(deliverableSummary);
                for (const row of deliverable.tasks) {
                    const taskNode = document.createElement('div');
                    taskNode.className = 'ml-4 border-l border-slate-200 pl-2 dark:border-slate-700';
                    taskNode.append(breakdownRow(t, 3, `${row.task.taskNumber || ''} ${row.task.title}`.trim(),
                        { budget: row.budget, estimate: row.estimate, actual: row.actual, billable: row.billable, nonBillable: row.nonBillable },
                        openTask, row.task.id));
                    deliverableDetails.append(taskNode);
                }
                projectDetails.append(deliverableDetails);
            }
            customerDetails.append(projectDetails);
        }
        panel.append(customerDetails);
    }
    const legend = document.createElement('div');
    legend.className = 'mt-3 flex flex-wrap gap-3 text-xs text-slate-600 dark:text-slate-300';
    for (const [color, label] of [['#10b981', t('kpi.billable', 'Abrechenbar')], ['#0ea5e9', t('kpi.nonBillable', 'Nicht abrechenbar')], ['#ef4444', t('kpi.overrun', 'Überschreitung')]]) {
        const item = document.createElement('span');
        const swatch = document.createElement('span');
        swatch.className = 'mr-1 inline-block h-2 w-2 rounded-sm';
        swatch.style.backgroundColor = color;
        item.append(swatch, document.createTextNode(label));
        legend.append(item);
    }
    panel.append(legend);
    return panel;
}

function renderCumulativeBurn(model, t) {
    const wrapper = document.createElement('div');
    const points = model.cumulative;
    if (!points.length) {
        wrapper.append(emptyChart(t));
        return wrapper;
    }
    const { budget, estimate } = model.totals;
    const maxValue = Math.max(points[points.length - 1].value, budget, estimate, 1);
    const minTime = points[0].time;
    const maxTime = Math.max(points[points.length - 1].time, minTime + 86400000);
    const x = time => 35 + (time - minTime) / (maxTime - minTime) * 315;
    const y = value => 170 - value / maxValue * 150;
    const svg = chartFrame(t('kpi.cumulativeBurn', 'Kumulierter Budgetverbrauch'));

    for (const [reference, color, label] of [[budget, '#0ea5e9', t('kpi.budgetShort', 'Budget')], [estimate, '#f59e0b', t('kpi.estimateShort', 'Schätzung')]]) {
        if (!(reference > 0)) continue;
        svg.append(svgElement('line', { x1: 35, y1: y(reference), x2: 350, y2: y(reference), stroke: color, 'stroke-dasharray': '4 3' }));
        addText(svg, 348, y(reference) - 3, `${label}: ${formatHours(reference)}`, { 'text-anchor': 'end', fill: color });
    }

    const path = points.map((point, index) => `${index === 0 ? 'M' : 'L'} ${x(point.time)} ${y(point.value)}`).join(' ');
    svg.append(svgElement('path', { d: `${path}`, fill: 'none', stroke: '#10b981', 'stroke-width': 2 }));
    svg.append(svgElement('path', {
        d: `${path} L ${x(points[points.length - 1].time)} 170 L ${x(points[0].time)} 170 Z`,
        fill: '#10b981', opacity: 0.15
    }));
    for (const point of points) {
        const dot = svgElement('circle', { cx: x(point.time), cy: y(point.value), r: 2.5, fill: '#10b981' });
        addTitle(dot, `${new Date(point.time).toISOString().slice(0, 10)}: ${formatHours(point.value)}`);
        svg.append(dot);
    }
    addText(svg, 30, 20, formatHours(maxValue), { 'text-anchor': 'end' });
    addText(svg, 35, 188, new Date(minTime).toISOString().slice(5, 10));
    addText(svg, 350, 188, new Date(maxTime).toISOString().slice(5, 10), { 'text-anchor': 'end' });
    wrapper.append(svg);
    return wrapper;
}

function renderWeeklyBurn(model, t) {
    const wrapper = document.createElement('div');
    const weeks = model.weekly;
    if (!weeks.length) {
        wrapper.append(emptyChart(t));
        return wrapper;
    }
    const max = Math.max(...weeks.map(week => week.billable + week.nonBillable), 1);
    const svg = chartFrame(t('kpi.weeklyBurn', 'Wöchentliche Burn Rate'));
    const slot = 315 / weeks.length;
    const barWidth = Math.min(28, slot * 0.6);
    weeks.forEach((week, index) => {
        const cx = 35 + slot * index + slot / 2;
        const billableHeight = week.billable / max * 150;
        const nonBillableHeight = week.nonBillable / max * 150;
        const billableBar = svgElement('rect', {
            x: cx - barWidth / 2, y: 170 - billableHeight, width: barWidth, height: Math.max(0, billableHeight), fill: '#10b981'
        });
        addTitle(billableBar, `${t('kpi.billable', 'Abrechenbar')}: ${formatHours(week.billable)}`);
        const nonBillableBar = svgElement('rect', {
            x: cx - barWidth / 2, y: 170 - billableHeight - nonBillableHeight, width: barWidth, height: Math.max(0, nonBillableHeight), fill: '#0ea5e9'
        });
        addTitle(nonBillableBar, `${t('kpi.nonBillable', 'Nicht abrechenbar')}: ${formatHours(week.nonBillable)}`);
        svg.append(billableBar, nonBillableBar);
        addText(svg, cx, 188, t('task.weekLabel', 'KW {week}', { week: week.week }), { 'text-anchor': 'middle' });
    });
    addText(svg, 30, 20, formatHours(max), { 'text-anchor': 'end' });
    wrapper.append(svg);
    const legend = document.createElement('div');
    legend.className = 'flex flex-wrap gap-2 text-xs text-slate-600 dark:text-slate-300';
    for (const [color, label] of [['#10b981', t('kpi.billable', 'Abrechenbar')], ['#0ea5e9', t('kpi.nonBillable', 'Nicht abrechenbar')]]) {
        const item = document.createElement('span');
        const swatch = document.createElement('span');
        swatch.className = 'mr-1 inline-block h-2 w-2';
        swatch.style.backgroundColor = color;
        item.append(swatch, document.createTextNode(label));
        legend.append(item);
    }
    wrapper.append(legend);
    return wrapper;
}

function renderRiskRanking(model, t, openTask) {
    const wrapper = document.createElement('div');
    if (!model.risk.length) {
        wrapper.append(emptyChart(t));
        return wrapper;
    }
    const list = document.createElement('div');
    list.className = 'mt-3 space-y-2';
    const maxUtilization = Math.max(...model.risk.map(item => item.utilization), 1);
    for (const item of model.risk) {
        const label = item.task.taskNumber || item.task.title;
        const row = document.createElement('button');
        row.type = 'button';
        row.className = 'w-full cursor-pointer rounded text-left text-xs text-slate-600 hover:text-blue-600 focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-500 dark:text-slate-300 dark:hover:text-blue-400';
        row.setAttribute('aria-label', t('kpi.openTask', 'Task {task} im Kanban öffnen', { task: label }));
        if (openTask) row.addEventListener('click', () => openTask(item.task.id));
        const head = document.createElement('div');
        head.className = 'flex justify-between gap-2';
        const name = document.createElement('span');
        name.textContent = `${label} · ${item.referenceKind === 'budget' ? t('kpi.budgetShort', 'Budget') : t('kpi.estimateShort', 'Schätzung')} ${formatHours(item.referenceHours)}`;
        const value = document.createElement('span');
        value.textContent = item.overrun > 0 ? `${formatPercent(item.utilization)} (+${formatHours(item.overrun)})` : formatPercent(item.utilization);
        head.append(name, value);
        const track = document.createElement('div');
        track.className = 'mt-1 h-2 rounded-full bg-slate-100 dark:bg-slate-800';
        const bar = document.createElement('div');
        bar.className = `h-full rounded-full ${item.utilization > 1 ? 'bg-red-500' : item.utilization >= 0.8 ? 'bg-amber-500' : 'bg-emerald-500'}`;
        bar.style.width = `${Math.min(100, item.utilization / maxUtilization * 100)}%`;
        track.append(bar);
        row.append(head, track);
        list.append(row);
    }
    wrapper.append(list);
    const note = document.createElement('p');
    note.className = 'mt-2 text-xs text-slate-500 dark:text-slate-400';
    note.textContent = t('kpi.riskNote', 'Aufgaben ohne Budget und Schätzung werden nicht bewertet.');
    wrapper.append(note);
    return wrapper;
}

function renderEstimateScatter(model, t, openTask) {
    const wrapper = document.createElement('div');
    if (!model.scatter.length) {
        wrapper.append(emptyChart(t));
        return wrapper;
    }
    const max = Math.max(...model.scatter.map(point => Math.max(point.estimate, point.actual)), 1);
    const x = value => 35 + value / max * 315;
    const y = value => 170 - value / max * 150;
    const svg = chartFrame(t('kpi.estimateScatter', 'Schätzung vs. Ist'), true);
    svg.append(svgElement('line', { x1: x(0), y1: y(0), x2: x(max), y2: y(max), stroke: '#ef4444', 'stroke-dasharray': '4 3' }));
    addText(svg, 348, y(max) + 12, t('kpi.parityLine', 'Ist = Schätzung'), { 'text-anchor': 'end', fill: '#ef4444' });
    for (const point of model.scatter) {
        const label = point.task.taskNumber || point.task.title;
        const dot = svgElement('circle', {
            cx: x(point.estimate), cy: y(point.actual), r: 4,
            fill: point.actual > point.estimate ? '#ef4444' : '#3b82f6',
            role: 'button', tabindex: 0,
            class: 'cursor-pointer focus-visible:outline focus-visible:outline-2 focus-visible:outline-blue-500',
            'aria-label': t('kpi.openTask', 'Task {task} im Kanban öffnen', { task: label })
        });
        addTitle(dot, `${label}: ${t('kpi.estimateShort', 'Schätzung')} ${formatHours(point.estimate)} → ${t('kpi.actualShort', 'Ist')} ${formatHours(point.actual)}`);
        if (openTask) {
            dot.addEventListener('click', () => openTask(point.task.id));
            dot.addEventListener('keydown', event => {
                if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); openTask(point.task.id); }
            });
        }
        svg.append(dot);
    }
    addText(svg, 30, 20, formatHours(max), { 'text-anchor': 'end' });
    addText(svg, 350, 188, formatHours(max), { 'text-anchor': 'end' });
    addText(svg, 35, 188, '0h');
    wrapper.append(svg);
    return wrapper;
}

/**
 * Renders the complete Budget KPIs tab into `root`.
 * context: { t, openTask, billableOnly, onBillableToggle, onOpenReport, statusText }
 */
export function renderBudgetKpis(root, model, context) {
    const { t, openTask, onBillableToggle, onOpenReport } = context;
    root.replaceChildren();

    // --- controls row: billable filter + billing report assistant ---
    const controls = document.createElement('div');
    controls.className = 'flex flex-wrap items-center justify-between gap-3';
    const toggleLabel = document.createElement('label');
    toggleLabel.className = 'inline-flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-200';
    const toggle = document.createElement('input');
    toggle.type = 'checkbox';
    toggle.checked = !!context.billableOnly;
    toggle.className = 'rounded border-slate-300 text-blue-600 focus:ring-blue-500';
    toggle.addEventListener('change', () => onBillableToggle?.(toggle.checked));
    toggleLabel.append(toggle, document.createTextNode(t('kpi.billableOnly', 'Nur abrechenbare Stunden')));
    const reportButton = document.createElement('button');
    reportButton.type = 'button';
    reportButton.className = 'rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500';
    reportButton.innerHTML = '<i class="fa-solid fa-file-invoice mr-2" aria-hidden="true"></i>';
    reportButton.append(document.createTextNode(t('kpi.billingAssistant', 'Abrechnungsreport-Assistent')));
    reportButton.addEventListener('click', () => onOpenReport?.());
    controls.append(toggleLabel, reportButton);
    root.append(controls);

    if (context.statusText) {
        const notice = document.createElement('p');
        notice.className = 'text-sm text-slate-600 dark:text-slate-400';
        notice.setAttribute('role', 'status');
        notice.textContent = context.statusText;
        root.append(notice);
        return;
    }

    const section = title => {
        const container = document.createElement('section');
        const heading = document.createElement('h3');
        heading.className = 'mb-3 text-base font-semibold text-slate-800 dark:text-slate-100';
        heading.textContent = title;
        container.append(heading);
        root.append(container);
        return container;
    };

    // --- row 1: hero KPI cards ---
    const totals = model.totals;
    const heroes = section(t('kpi.budgetMetrics', 'Budget-Kennzahlen'));
    const heroGrid = document.createElement('div');
    heroGrid.className = 'grid gap-3 sm:grid-cols-2 xl:grid-cols-4';
    heroGrid.append(
        heroCard(t('kpi.budgetBurn', 'Budget Burn'), formatPercent(totals.burn), burnColor(totals.burn),
            t('kpi.budgetBurnExplanation', 'Erfasste Ist-Stunden im Verhältnis zum zugewiesenen Budget (Summe der Lieferobjekt-Budgets im Filter).'),
            `${formatHours(totals.actual)} / ${formatHours(totals.budget > 0 ? totals.budget : null)}`),
        heroCard(t('kpi.remainingBudget', 'Restbudget'), formatHours(totals.remaining),
            totals.remaining !== null && totals.remaining < 0 ? 'text-red-600 dark:text-red-400' : '',
            t('kpi.remainingBudgetExplanation', 'Zugewiesenes Budget abzüglich erfasster Ist-Stunden. Negative Werte zeigen eine Überschreitung.'),
            t('kpi.remainingBudgetNote', 'Budget − Ist')),
        heroCard(t('kpi.unallocatedBudget', 'Nicht allokiertes Budget'), formatHours(totals.unallocated),
            totals.unallocated !== null && totals.unallocated < 0 ? 'text-amber-600 dark:text-amber-400' : '',
            t('kpi.unallocatedExplanation', 'Lieferobjekt-Budgets abzüglich der auf Tasks verteilten Budgets. Negative Werte bedeuten Überallokation.'),
            `${formatHours(totals.budget > 0 ? totals.budget : null)} − ${formatHours(totals.taskBudget > 0 ? totals.taskBudget : null)}`),
        heroCard(t('kpi.billableShare', 'Abrechenbarer Anteil'), formatPercent(totals.billableShare), '',
            t('kpi.billableShareExplanation', 'Anteil der abrechenbaren Stunden an allen erfassten Stunden (unabhängig vom Abrechenbar-Filter).'),
            `${formatHours(totals.billableActual)} / ${formatHours(totals.billableActual + totals.nonBillableActual)}`)
    );
    heroes.append(heroGrid);

    // --- row 2: hierarchy breakdown ---
    const breakdown = section(t('kpi.budgetBreakdown', 'Budgetaufschlüsselung'));
    const breakdownNote = document.createElement('p');
    breakdownNote.className = 'mb-3 text-xs text-slate-500 dark:text-slate-400';
    breakdownNote.textContent = t('kpi.budgetBreakdownNote', 'Budget, Schätzung und Ist-Stunden je Kunde, Projekt, Lieferobjekt und Task. Balken zeigen abrechenbare und nicht abrechenbare Anteile relativ zum Budget (oder zur Schätzung).');
    breakdown.append(breakdownNote, renderBreakdown(model, t, openTask));

    // --- row 3: analytical charts ---
    const charts = section(t('kpi.budgetCharts', 'Verlaufsdiagramme'));
    const chartGrid = document.createElement('div');
    chartGrid.className = 'grid gap-3 sm:grid-cols-2 xl:grid-cols-4';
    const cumulativeCard = chartCard(t('kpi.cumulativeBurn', 'Kumulierter Budgetverbrauch'),
        t('kpi.cumulativeBurnExplanation', 'Kumulierte Ist-Stunden im Zeitverlauf gegenüber Budget- und Schätzungslinien.'));
    cumulativeCard.append(renderCumulativeBurn(model, t));
    const weeklyCard = chartCard(t('kpi.weeklyBurn', 'Wöchentliche Burn Rate'),
        t('kpi.weeklyBurnExplanation', 'Erfasste Stunden je Kalenderwoche, aufgeteilt nach abrechenbar und nicht abrechenbar.'));
    weeklyCard.append(renderWeeklyBurn(model, t));
    const riskCard = chartCard(t('kpi.riskRanking', 'Überschreitungs-Ranking'),
        t('kpi.riskRankingExplanation', 'Tasks mit der höchsten Auslastung ihres Budgets bzw. ihrer Schätzung. Rot markierte Balken überschreiten die Referenz.'));
    riskCard.append(renderRiskRanking(model, t, openTask));
    const scatterCard = chartCard(t('kpi.estimateScatter', 'Schätzung vs. Ist'),
        t('kpi.estimateScatterExplanation', 'Jede Aufgabe als Punkt: x = Schätzung, y = Ist-Stunden. Punkte über der Diagonalen wurden unterschätzt.'));
    scatterCard.append(renderEstimateScatter(model, t, openTask));
    chartGrid.append(cumulativeCard, weeklyCard, riskCard, scatterCard);
    charts.append(chartGrid);

    const coverage = document.createElement('p');
    coverage.className = 'mt-3 text-xs text-slate-500 dark:text-slate-400';
    coverage.textContent = t('kpi.budgetCoverage', 'Auswertung nach aktuellen Filtern. Budgets stammen von den Lieferobjekten der gefilterten Aufgaben; Ist-Stunden aus den Zeiteinträgen der Aufgaben.');
    root.append(coverage);
}

/**
 * Placeholder drawer/modal for the upcoming Billing Report Assistant.
 * Receives the active filters and aggregated totals so the follow-up
 * iteration (pre-flight checks, AI summary, export) can build on it.
 */
export function openBillingReportModal({ t, filters, totals }) {
    document.getElementById('billing-report-modal')?.remove();
    const overlay = document.createElement('div');
    overlay.id = 'billing-report-modal';
    overlay.className = 'fixed inset-0 z-[70] flex items-center justify-center bg-slate-900/60 p-4';
    overlay.addEventListener('click', event => { if (event.target === overlay) overlay.remove(); });

    const modal = document.createElement('div');
    modal.className = 'w-full max-w-lg rounded-2xl border border-slate-200 bg-white p-6 shadow-xl dark:border-slate-700 dark:bg-slate-900';
    modal.setAttribute('role', 'dialog');
    modal.setAttribute('aria-modal', 'true');

    const header = document.createElement('div');
    header.className = 'flex items-start justify-between gap-3';
    const title = document.createElement('h3');
    title.className = 'text-lg font-bold text-slate-900 dark:text-white';
    title.textContent = t('kpi.billingAssistant', 'Abrechnungsreport-Assistent');
    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'rounded p-1 text-slate-500 hover:text-slate-900 dark:hover:text-white';
    close.setAttribute('aria-label', t('common.close', 'Schließen'));
    close.innerHTML = '<i class="fa-solid fa-xmark" aria-hidden="true"></i>';
    close.addEventListener('click', () => overlay.remove());
    header.append(title, close);

    const intro = document.createElement('p');
    intro.className = 'mt-2 text-sm text-slate-600 dark:text-slate-300';
    intro.textContent = t('kpi.billingAssistantIntro', 'Erstellt demnächst Abrechnungsreports auf Basis der aktiven Filter. Vorab-Prüfungen, KI-Zusammenfassung und Export folgen.');

    const filterBox = document.createElement('div');
    filterBox.className = 'mt-4 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs dark:border-slate-700 dark:bg-slate-800';
    const filterTitle = document.createElement('h4');
    filterTitle.className = 'mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500 dark:text-slate-400';
    filterTitle.textContent = t('kpi.activeFilters', 'Aktive Filter');
    filterBox.append(filterTitle);
    for (const [label, value] of filters) {
        const row = document.createElement('div');
        row.className = 'flex justify-between gap-3 py-0.5 text-slate-700 dark:text-slate-200';
        const key = document.createElement('span');
        key.className = 'text-slate-500 dark:text-slate-400';
        key.textContent = label;
        const val = document.createElement('span');
        val.className = 'text-right font-medium';
        val.textContent = value;
        row.append(key, val);
        filterBox.append(row);
    }

    const totalsBox = filterBox.cloneNode(false);
    totalsBox.className = filterBox.className + ' mt-3';
    const totalsTitle = filterTitle.cloneNode(false);
    totalsTitle.className = filterTitle.className;
    totalsTitle.textContent = t('kpi.reportScope', 'Report-Umfang');
    totalsBox.append(totalsTitle);
    for (const [label, value] of totals) {
        const row = document.createElement('div');
        row.className = 'flex justify-between gap-3 py-0.5 text-slate-700 dark:text-slate-200';
        const key = document.createElement('span');
        key.className = 'text-slate-500 dark:text-slate-400';
        key.textContent = label;
        const val = document.createElement('span');
        val.className = 'text-right font-medium tabular-nums';
        val.textContent = value;
        row.append(key, val);
        totalsBox.append(row);
    }

    const roadmap = document.createElement('ul');
    roadmap.className = 'mt-4 space-y-1 text-xs text-slate-500 dark:text-slate-400';
    for (const item of [
        t('kpi.assistantStepChecks', 'Vorab-Prüfungen (fehlende Kommentare, nicht klassifizierte Einträge)'),
        t('kpi.assistantStepSummary', 'KI-Zusammenfassung der erbrachten Leistungen'),
        t('kpi.assistantStepExport', 'Export als PDF/Excel')
    ]) {
        const li = document.createElement('li');
        li.innerHTML = '<i class="fa-regular fa-circle mr-2 text-slate-400" aria-hidden="true"></i>';
        li.append(document.createTextNode(item));
        roadmap.append(li);
    }

    modal.append(header, intro, filterBox, totalsBox, roadmap);
    overlay.append(modal);
    document.body.append(overlay);
    close.focus();
}

export { formatHours, formatPercent, isoWeek };
