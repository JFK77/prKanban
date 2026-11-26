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
    if (value === null || !Number.isFinite(value)) return '—';
    let percent = Math.round(value * 1000) / 10;
    // Never display a rounded 100% / 0% when the true value differs (e.g. 99.76% must not appear as 100%).
    if (percent === 100 && value < 1) percent = 99.9;
    else if (percent === 0 && value > 0) percent = 0.1;
    return `${percent.toLocaleString('de-DE', { maximumFractionDigits: 1 })}%`;
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
    const svg = chartFrame(t('kpi.cumulativeBurn', 'Kumulierter Budgetverbrauch'), false,
        { x: t('kpi.axisDate', 'Datum'), y: t('kpi.axisHours', 'Stunden (h)') });

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
    const svg = chartFrame(t('kpi.weeklyBurn', 'Wöchentliche Burn Rate'), false,
        { x: t('kpi.axisWeek', 'Kalenderwoche'), y: t('kpi.axisHours', 'Stunden (h)') });
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
    const svg = chartFrame(t('kpi.estimateScatter', 'Schätzung vs. Ist'), true,
        { x: t('kpi.axisEstimate', 'Schätzung (h)'), y: t('kpi.axisActual', 'Ist (h)') });
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

// ---------------------------------------------------------------------------
// Billing Report Assistant
// ---------------------------------------------------------------------------

const LOGO_STORAGE_KEY = 'billing_report_logo';
const CREATOR_STORAGE_KEY = 'billing_report_creator';

function parseDateInput(value) {
    const time = Date.parse(`${value}T00:00:00Z`);
    return Number.isFinite(time) ? time : NaN;
}

function formatDateDE(time) {
    const date = new Date(time);
    return `${String(date.getUTCDate()).padStart(2, '0')}.${String(date.getUTCMonth() + 1).padStart(2, '0')}.${date.getUTCFullYear()}`;
}

function isoDate(time) {
    return new Date(time).toISOString().slice(0, 10);
}

/**
 * Resolves the assistant scope and filters time entries accordingly.
 * Pure and defensive: invalid ranges and missing data yield empty results.
 * Returns { rows, invalidRange, scopedTasks, selectedDeliverables, users }.
 */
export function collectReportData({ hierarchy, tasks, entriesByTask }, state) {
    const projects = (hierarchy.projects || []).filter(project => !state.customerId || project.customerId === state.customerId);
    const projectIds = new Set(projects.map(project => project.id));
    const deliverables = (hierarchy.deliverables || [])
        .filter(item => projectIds.has(item.projectId))
        .filter(item => !state.projectId || item.projectId === state.projectId);
    const selectedDeliverables = state.deliverableIds.size
        ? deliverables.filter(item => state.deliverableIds.has(item.id))
        : deliverables;
    const selectedIds = new Set(selectedDeliverables.map(item => item.id));
    const scopedTasks = (tasks || []).filter(task => task.deliverableId && selectedIds.has(task.deliverableId));

    const from = state.from ? parseDateInput(state.from) : NaN;
    const to = state.to ? parseDateInput(state.to) + 86399999 : NaN;
    const invalidRange = Number.isFinite(from) && Number.isFinite(to) && from > to;

    const rows = [];
    const users = new Set();
    if (!invalidRange) {
        for (const task of scopedTasks) {
            for (const entry of entriesByTask?.get(task.id) || []) {
                const time = utcTime(entry.startTime);
                if (!Number.isFinite(time)) continue;
                if (entry.userEmail) users.add(entry.userEmail);
                if (Number.isFinite(from) && time < from) continue;
                if (Number.isFinite(to) && time > to) continue;
                if (state.userEmail && entry.userEmail !== state.userEmail) continue;
                if (!state.includeNonBillable && !entry.isBillable) continue;
                rows.push({ entry, task, time });
            }
        }
    }
    rows.sort((a, b) => a.time - b.time || String(a.entry.id).localeCompare(String(b.entry.id)));

    const budget = selectedDeliverables.reduce((sum, item) => sum + toNumber(item.budgetHours), 0);
    const billable = rows.reduce((sum, row) => sum + (row.entry.isBillable ? toNumber(row.entry.durationHours) : 0), 0);
    const nonBillable = rows.reduce((sum, row) => sum + (!row.entry.isBillable ? toNumber(row.entry.durationHours) : 0), 0);
    const actual = billable + nonBillable;

    return {
        rows, invalidRange, scopedTasks, selectedDeliverables,
        users: [...users].sort(),
        summary: { budget, actual, billable, nonBillable, burn: ratio(actual, budget > 0 ? budget : 0) }
    };
}

function loadScriptOnce(src) {
    return new Promise((resolve, reject) => {
        if (document.querySelector(`script[src="${src}"]`)) return resolve();
        const script = document.createElement('script');
        script.src = src;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error(`Failed to load ${src}`));
        document.head.append(script);
    });
}

async function ensureJsPdf() {
    if (!window.jspdf?.jsPDF)
        await loadScriptOnce('https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js');
    if (!window.jspdf.jsPDF.API.autoTable)
        await loadScriptOnce('https://cdnjs.cloudflare.com/ajax/libs/jspdf-autotable/3.8.2/jspdf.plugin.autotable.min.js');
    return window.jspdf.jsPDF;
}

function loadImageDimensions(dataUrl) {
    return new Promise(resolve => {
        const image = new Image();
        image.onload = () => resolve({ width: image.naturalWidth, height: image.naturalHeight });
        image.onerror = () => resolve(null);
        image.src = dataUrl;
    });
}

function downloadBlob(blob, fileName) {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
}

/**
 * Billing Report Assistant modal: scope, period and display controls with a
 * live summary + entry preview, period locking and Excel/PDF export.
 *
 * context: {
 *   t, currentUser, hierarchy: {customers, projects, deliverables}, tasks,
 *   fetchEntries(tasks) => Promise<Map>, clearEntryCache(), onLocksChanged(),
 *   defaults: { customerId, projectId, includeNonBillable }
 * }
 */
export function openBillingReportModal(context) {
    const { t, hierarchy, tasks, defaults = {} } = context;
    document.getElementById('billing-report-modal')?.remove();

    const today = new Date();
    const monthStart = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-01`;
    const state = {
        customerId: defaults.customerId || '',
        projectId: defaults.projectId || '',
        deliverableIds: new Set(),
        from: monthStart,
        to: today.toISOString().slice(0, 10),
        userEmail: '',
        includeNonBillable: defaults.includeNonBillable !== false,
        showComments: true,
        showUsers: true,
        creator: localStorage.getItem(CREATOR_STORAGE_KEY) || context.currentUser || '',
        logo: localStorage.getItem(LOGO_STORAGE_KEY) || ''
    };
    let entriesByTask = new Map();
    let data = collectReportData({ hierarchy, tasks, entriesByTask }, state);
    let loading = true;

    const overlay = document.createElement('div');
    overlay.id = 'billing-report-modal';
    overlay.className = 'fixed inset-0 z-[70] flex items-center justify-center bg-slate-900/60 p-4';
    overlay.addEventListener('click', event => { if (event.target === overlay) overlay.remove(); });

    const modal = document.createElement('div');
    modal.className = 'flex max-h-[92vh] w-full max-w-5xl flex-col rounded-2xl border border-slate-200 bg-white shadow-xl dark:border-slate-700 dark:bg-slate-900';
    modal.setAttribute('role', 'dialog');
    modal.setAttribute('aria-modal', 'true');

    // --- header ---
    const header = document.createElement('div');
    header.className = 'flex items-start justify-between gap-3 border-b border-slate-200 p-5 dark:border-slate-700';
    const title = document.createElement('h3');
    title.className = 'text-lg font-bold text-slate-900 dark:text-white';
    title.innerHTML = '<i class="fa-solid fa-file-invoice mr-2 text-blue-600" aria-hidden="true"></i>';
    title.append(document.createTextNode(t('kpi.billingAssistant', 'Abrechnungsreport-Assistent')));
    const close = document.createElement('button');
    close.type = 'button';
    close.className = 'rounded p-1 text-slate-500 hover:text-slate-900 dark:hover:text-white';
    close.setAttribute('aria-label', t('common.close', 'Schließen'));
    close.innerHTML = '<i class="fa-solid fa-xmark" aria-hidden="true"></i>';
    close.addEventListener('click', () => overlay.remove());
    header.append(title, close);

    // --- body (scrollable) ---
    const body = document.createElement('div');
    body.className = 'min-h-0 flex-1 space-y-4 overflow-y-auto p-5';

    const selectClass = 'w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm text-slate-800 focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-slate-700 dark:bg-slate-800 dark:text-white';
    const labelClass = 'block text-xs font-semibold text-slate-600 dark:text-slate-300';
    const field = (labelText, control) => {
        const wrap = document.createElement('div');
        const label = document.createElement('label');
        label.className = labelClass + ' mb-1';
        label.textContent = labelText;
        wrap.append(label, control);
        return wrap;
    };
    const checkboxField = (labelText, checked, onChange) => {
        const label = document.createElement('label');
        label.className = 'inline-flex cursor-pointer items-center gap-2 text-sm text-slate-700 dark:text-slate-200';
        const input = document.createElement('input');
        input.type = 'checkbox';
        input.checked = checked;
        input.className = 'rounded border-slate-300 text-blue-600 focus:ring-blue-500';
        input.addEventListener('change', () => onChange(input.checked));
        label.append(input, document.createTextNode(labelText));
        return label;
    };

    // scope controls
    const scopeGrid = document.createElement('div');
    scopeGrid.className = 'grid gap-3 sm:grid-cols-2 lg:grid-cols-3';

    const customerSelect = document.createElement('select');
    customerSelect.className = selectClass;
    const projectSelect = document.createElement('select');
    projectSelect.className = selectClass;
    const deliverableBox = document.createElement('div');
    deliverableBox.className = 'max-h-28 space-y-1 overflow-y-auto rounded-lg border border-slate-300 bg-white p-2 text-sm dark:border-slate-700 dark:bg-slate-800';
    const fromInput = document.createElement('input');
    fromInput.type = 'date';
    fromInput.className = selectClass;
    fromInput.value = state.from;
    const toInput = document.createElement('input');
    toInput.type = 'date';
    toInput.className = selectClass;
    toInput.value = state.to;
    const userSelect = document.createElement('select');
    userSelect.className = selectClass;

    scopeGrid.append(
        field(t('hierarchy.customer', 'Kunde'), customerSelect),
        field(t('hierarchy.project', 'Projekt'), projectSelect),
        field(t('hierarchy.deliverables', 'Lieferobjekte'), deliverableBox),
        field(t('kpi.reportFrom', 'Von'), fromInput),
        field(t('kpi.reportTo', 'Bis'), toInput),
        field(t('kpi.reportUser', 'Benutzer'), userSelect)
    );

    // display toggles + settings
    const toggleRow = document.createElement('div');
    toggleRow.className = 'flex flex-wrap items-center gap-x-6 gap-y-2 rounded-lg border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-800/50';
    toggleRow.append(
        checkboxField(t('kpi.includeNonBillable', 'Non-Billable einbeziehen'), state.includeNonBillable, value => { state.includeNonBillable = value; refresh(); }),
        checkboxField(t('kpi.showComments', 'Kommentare anzeigen'), state.showComments, value => { state.showComments = value; refresh(); }),
        checkboxField(t('kpi.showUserNames', 'Benutzernamen anzeigen'), state.showUsers, value => { state.showUsers = value; refresh(); })
    );

    const settingsGrid = document.createElement('div');
    settingsGrid.className = 'grid gap-3 sm:grid-cols-2';
    const creatorInput = document.createElement('input');
    creatorInput.type = 'text';
    creatorInput.className = selectClass;
    creatorInput.value = state.creator;
    creatorInput.addEventListener('change', () => {
        state.creator = creatorInput.value.trim();
        localStorage.setItem(CREATOR_STORAGE_KEY, state.creator);
    });
    const logoWrap = document.createElement('div');
    logoWrap.className = 'flex items-center gap-3';
    const logoPreview = document.createElement('img');
    logoPreview.className = 'h-9 max-w-[8rem] rounded border border-slate-200 object-contain p-1 dark:border-slate-700';
    logoPreview.alt = t('kpi.companyLogo', 'Firmenlogo');
    const logoInput = document.createElement('input');
    logoInput.type = 'file';
    logoInput.accept = 'image/png,image/jpeg';
    logoInput.className = 'text-xs text-slate-500 dark:text-slate-400';
    const logoRemove = document.createElement('button');
    logoRemove.type = 'button';
    logoRemove.className = 'text-xs text-red-600 hover:underline dark:text-red-400';
    logoRemove.textContent = t('common.remove', 'Entfernen');
    const refreshLogo = () => {
        logoPreview.classList.toggle('hidden', !state.logo);
        logoRemove.classList.toggle('hidden', !state.logo);
        if (state.logo) logoPreview.src = state.logo;
    };
    logoInput.addEventListener('change', () => {
        const file = logoInput.files?.[0];
        if (!file) return;
        const reader = new FileReader();
        reader.onload = () => {
            state.logo = String(reader.result);
            try { localStorage.setItem(LOGO_STORAGE_KEY, state.logo); } catch { /* quota exceeded: keep in-memory */ }
            refreshLogo();
        };
        reader.readAsDataURL(file);
    });
    logoRemove.addEventListener('click', () => {
        state.logo = '';
        localStorage.removeItem(LOGO_STORAGE_KEY);
        logoInput.value = '';
        refreshLogo();
    });
    logoWrap.append(logoInput, logoPreview, logoRemove);
    refreshLogo();
    settingsGrid.append(
        field(t('kpi.reportCreator', 'Ersteller des Reports'), creatorInput),
        field(t('kpi.companyLogo', 'Firmenlogo (PDF, oben rechts)'), logoWrap)
    );

    // summary cards
    const summaryGrid = document.createElement('div');
    summaryGrid.className = 'grid gap-3 sm:grid-cols-3 lg:grid-cols-5';

    // preview table
    const previewWrap = document.createElement('div');
    previewWrap.className = 'rounded-lg border border-slate-200 dark:border-slate-700';
    const previewScroll = document.createElement('div');
    previewScroll.className = 'max-h-64 overflow-y-auto';
    previewWrap.append(previewScroll);

    const statusLine = document.createElement('p');
    statusLine.className = 'text-xs text-slate-500 dark:text-slate-400';
    statusLine.setAttribute('role', 'status');

    body.append(scopeGrid, toggleRow, settingsGrid, summaryGrid, previewWrap, statusLine);

    // --- footer actions ---
    const footer = document.createElement('div');
    footer.className = 'flex flex-wrap items-center justify-end gap-2 border-t border-slate-200 p-4 dark:border-slate-700';
    const lockButton = document.createElement('button');
    lockButton.type = 'button';
    lockButton.className = 'rounded-lg bg-amber-600 px-4 py-2 text-sm font-semibold text-white hover:bg-amber-500 disabled:cursor-not-allowed disabled:opacity-50';
    lockButton.innerHTML = '<i class="fa-solid fa-lock mr-2" aria-hidden="true"></i>';
    lockButton.append(document.createTextNode(t('kpi.lockPeriod', 'Zeiten in der Periode sperren')));
    const excelButton = document.createElement('button');
    excelButton.type = 'button';
    excelButton.className = 'rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-500 disabled:cursor-not-allowed disabled:opacity-50';
    excelButton.innerHTML = '<i class="fa-solid fa-file-excel mr-2" aria-hidden="true"></i>';
    excelButton.append(document.createTextNode(t('kpi.exportExcel', 'Export nach Excel')));
    const pdfButton = document.createElement('button');
    pdfButton.type = 'button';
    pdfButton.className = 'rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-500 disabled:cursor-not-allowed disabled:opacity-50';
    pdfButton.innerHTML = '<i class="fa-solid fa-file-pdf mr-2" aria-hidden="true"></i>';
    pdfButton.append(document.createTextNode(t('kpi.exportPdf', 'Export als PDF')));
    footer.append(lockButton, excelButton, pdfButton);

    modal.append(header, body, footer);
    overlay.append(modal);
    document.body.append(overlay);

    // --- helpers bound to current state ---
    const customerName = () => (hierarchy.customers || []).find(item => item.id === state.customerId)?.name
        || t('board.allCustomers', 'Alle Kunden');
    const customerDebitor = () => (hierarchy.customers || []).find(item => item.id === state.customerId)?.debitorNo || '';
    const projectName = () => (hierarchy.projects || []).find(item => item.id === state.projectId)?.name
        || t('projectBoard.allProjects', 'Alle Projekte');
    const deliverableLabel = () => state.deliverableIds.size
        ? data.selectedDeliverables.map(item => item.name).join(', ')
        : t('kpi.allDeliverables', 'Alle Lieferobjekte');
    const periodLabel = () => `${state.from ? formatDateDE(parseDateInput(state.from)) : '—'} – ${state.to ? formatDateDE(parseDateInput(state.to)) : '—'}`;

    function summaryPairs() {
        return [
            [t('kpi.budgetShort', 'Budget'), formatHours(data.summary.budget > 0 ? data.summary.budget : null)],
            [t('kpi.actualShort', 'Ist'), formatHours(data.summary.actual)],
            [t('kpi.billable', 'Abrechenbar'), formatHours(data.summary.billable)],
            [t('kpi.nonBillable', 'Nicht abrechenbar'), formatHours(data.summary.nonBillable)],
            [t('kpi.budgetBurn', 'Budget Burn'), formatPercent(data.summary.burn)]
        ];
    }

    function metadataPairs() {
        const pairs = [
            [t('hierarchy.customer', 'Kunde'), customerName()],
            [t('hierarchy.project', 'Projekt'), projectName()],
            [t('hierarchy.deliverables', 'Lieferobjekte'), deliverableLabel()],
            [t('kpi.reportPeriod', 'Zeitraum'), periodLabel()],
            [t('kpi.reportUser', 'Benutzer'), state.userEmail || t('kpi.filterAll', 'Alle')],
            [t('kpi.reportDate', 'Datum'), formatDateDE(Date.now())],
            [t('kpi.reportCreator', 'Ersteller des Reports'), state.creator || '—']
        ];
        if (customerDebitor()) pairs.splice(1, 0, [t('hierarchy.debitorNo', 'Debitorennummer'), customerDebitor()]);
        return pairs;
    }

    function entryHeaders() {
        const headers = [t('task.date', 'Datum'), t('hierarchy.deliverable', 'Lieferobjekt'), t('task.title', 'Task'),
            t('task.timeDuration', 'Dauer'), t('kpi.billable', 'Abrechenbar'), t('kpi.lockedColumn', 'Gesperrt')];
        if (state.showUsers) headers.push(t('task.timeUser', 'Benutzer'));
        if (state.showComments) headers.push(t('task.timeComment', 'Kommentar'));
        return headers;
    }

    function entryRows() {
        return data.rows.map(({ entry, task, time }) => {
            const row = [formatDateDE(time), task.deliverableName || '', `${task.taskNumber || ''} ${task.title}`.trim(),
                formatHours(toNumber(entry.durationHours)),
                entry.isBillable ? t('common.yes', 'Ja') : t('common.no', 'Nein'),
                entry.locked ? t('common.yes', 'Ja') : t('common.no', 'Nein')];
            if (state.showUsers) row.push(entry.userEmail || '—');
            if (state.showComments) row.push(entry.comment || '—');
            return row;
        });
    }

    function renderCustomerOptions() {
        customerSelect.replaceChildren(new Option(t('board.allCustomers', 'Alle Kunden'), ''));
        for (const customer of hierarchy.customers || [])
            customerSelect.add(new Option(customer.name, customer.id));
        customerSelect.value = state.customerId;
    }

    function renderProjectOptions() {
        projectSelect.replaceChildren(new Option(t('projectBoard.allProjects', 'Alle Projekte'), ''));
        for (const project of (hierarchy.projects || []).filter(item => !state.customerId || item.customerId === state.customerId))
            projectSelect.add(new Option(project.name, project.id));
        projectSelect.value = [...projectSelect.options].some(option => option.value === state.projectId) ? state.projectId : '';
        state.projectId = projectSelect.value;
    }

    function renderDeliverableOptions() {
        const projects = new Set((hierarchy.projects || [])
            .filter(item => !state.customerId || item.customerId === state.customerId)
            .filter(item => !state.projectId || item.id === state.projectId)
            .map(item => item.id));
        const options = (hierarchy.deliverables || []).filter(item => projects.has(item.projectId));
        for (const id of [...state.deliverableIds])
            if (!options.some(item => item.id === id)) state.deliverableIds.delete(id);
        deliverableBox.replaceChildren();
        if (!options.length) {
            const empty = document.createElement('p');
            empty.className = 'text-xs text-slate-400';
            empty.textContent = '—';
            deliverableBox.append(empty);
            return;
        }
        deliverableBox.append(checkboxField(t('kpi.allDeliverables', 'Alle Lieferobjekte'), state.deliverableIds.size === 0, checked => {
            if (checked) state.deliverableIds.clear();
            renderDeliverableOptions();
            refresh();
        }));
        for (const item of options) {
            deliverableBox.append(checkboxField(item.name, state.deliverableIds.has(item.id), checked => {
                if (checked) state.deliverableIds.add(item.id);
                else state.deliverableIds.delete(item.id);
                renderDeliverableOptions();
                refresh();
            }));
        }
    }

    function renderUserOptions() {
        const current = state.userEmail;
        userSelect.replaceChildren(new Option(t('kpi.allUsers', 'Alle Benutzer'), ''));
        for (const email of data.users) userSelect.add(new Option(email, email));
        userSelect.value = data.users.includes(current) ? current : '';
        state.userEmail = userSelect.value;
    }

    function renderSummary() {
        summaryGrid.replaceChildren();
        for (const [label, value] of summaryPairs()) {
            const card = document.createElement('div');
            card.className = 'rounded-lg border border-slate-200 bg-slate-50 p-3 dark:border-slate-700 dark:bg-slate-800/50';
            const name = document.createElement('p');
            name.className = 'text-xs text-slate-500 dark:text-slate-400';
            name.textContent = label;
            const figure = document.createElement('p');
            figure.className = 'mt-1 text-lg font-bold tabular-nums text-slate-900 dark:text-white';
            figure.textContent = value;
            card.append(name, figure);
            summaryGrid.append(card);
        }
    }

    function renderPreview() {
        previewScroll.replaceChildren();
        if (data.invalidRange) {
            const warning = document.createElement('p');
            warning.className = 'p-4 text-sm text-amber-700 dark:text-amber-300';
            warning.textContent = t('kpi.invalidRange', 'Ungültiger Zeitraum: "Von" liegt nach "Bis".');
            previewScroll.append(warning);
            return;
        }
        if (loading) {
            const info = document.createElement('p');
            info.className = 'p-4 text-sm text-slate-500 dark:text-slate-400';
            info.textContent = t('kpi.loading', 'Aufgaben werden geladen …');
            previewScroll.append(info);
            return;
        }
        if (!data.rows.length) {
            const info = document.createElement('p');
            info.className = 'p-4 text-sm text-slate-500 dark:text-slate-400';
            info.textContent = t('kpi.noEntriesInScope', 'Keine Zeiteinträge im gewählten Umfang.');
            previewScroll.append(info);
            return;
        }
        const table = document.createElement('table');
        table.className = 'w-full border-collapse text-xs';
        const thead = document.createElement('thead');
        const headRow = document.createElement('tr');
        headRow.className = 'sticky top-0 border-b border-slate-300 bg-slate-100 text-left dark:border-slate-600 dark:bg-slate-800';
        for (const heading of entryHeaders()) {
            const th = document.createElement('th');
            th.className = 'px-2 py-1.5 font-semibold text-slate-900 dark:text-white';
            th.textContent = heading;
            headRow.append(th);
        }
        thead.append(headRow);
        const tbody = document.createElement('tbody');
        for (const { entry, task, time } of data.rows) {
            const tr = document.createElement('tr');
            tr.className = 'border-b border-slate-200 dark:border-slate-700';
            const cells = [formatDateDE(time), task.deliverableName || '', `${task.taskNumber || ''} ${task.title}`.trim(),
                formatHours(toNumber(entry.durationHours))];
            for (const value of cells) {
                const td = document.createElement('td');
                td.className = 'px-2 py-1.5 text-slate-700 dark:text-slate-300';
                td.textContent = value;
                tr.append(td);
            }
            const billableCell = document.createElement('td');
            billableCell.className = 'px-2 py-1.5';
            const badge = document.createElement('span');
            badge.className = `inline-block rounded px-1.5 py-0.5 text-[10px] font-medium ${entry.isBillable
                ? 'bg-emerald-100 text-emerald-800 dark:bg-emerald-900 dark:text-emerald-200'
                : 'bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-300'}`;
            badge.textContent = entry.isBillable ? t('kpi.billable', 'Abrechenbar') : t('kpi.nonBillable', 'Nicht abrechenbar');
            billableCell.append(badge);
            tr.append(billableCell);
            const lockCell = document.createElement('td');
            lockCell.className = 'px-2 py-1.5 text-center';
            lockCell.innerHTML = entry.locked
                ? '<i class="fa-solid fa-lock text-amber-500" aria-hidden="true"></i>'
                : '<i class="fa-solid fa-lock-open text-slate-300 dark:text-slate-600" aria-hidden="true"></i>';
            lockCell.title = entry.locked ? (entry.lockedPeriod || t('kpi.lockedColumn', 'Gesperrt')) : '';
            tr.append(lockCell);
            if (state.showUsers) {
                const td = document.createElement('td');
                td.className = 'max-w-[9rem] truncate px-2 py-1.5 text-slate-600 dark:text-slate-400';
                td.textContent = entry.userEmail || '—';
                td.title = entry.userEmail || '';
                tr.append(td);
            }
            if (state.showComments) {
                const td = document.createElement('td');
                td.className = 'max-w-[12rem] truncate px-2 py-1.5 text-slate-600 dark:text-slate-400';
                td.textContent = entry.comment || '—';
                td.title = entry.comment || '';
                tr.append(td);
            }
            tbody.append(tr);
        }
        table.append(thead, tbody);
        previewScroll.append(table);
    }

    function refresh() {
        data = collectReportData({ hierarchy, tasks, entriesByTask }, state);
        renderUserOptions();
        renderSummary();
        renderPreview();
        const exportable = !loading && !data.invalidRange && data.rows.length > 0;
        lockButton.disabled = !exportable;
        excelButton.disabled = !exportable;
        pdfButton.disabled = !exportable;
        if (!data.invalidRange)
            statusLine.textContent = loading ? '' : t('kpi.previewCount', '{count} Zeiteinträge im gewählten Umfang.', { count: data.rows.length });
    }

    async function reloadEntries() {
        loading = true;
        refresh();
        try {
            const scope = collectReportData({ hierarchy, tasks, entriesByTask: new Map() }, state);
            entriesByTask = await context.fetchEntries(scope.scopedTasks);
        } catch (error) {
            console.error('Billing assistant entries failed:', error);
            statusLine.textContent = t('kpi.loadFailed', 'Kennzahlen nicht verfügbar: Aufgaben konnten nicht geladen werden.');
        }
        loading = false;
        refresh();
    }

    customerSelect.addEventListener('change', () => {
        state.customerId = customerSelect.value;
        state.deliverableIds.clear();
        renderProjectOptions();
        renderDeliverableOptions();
        reloadEntries();
    });
    projectSelect.addEventListener('change', () => {
        state.projectId = projectSelect.value;
        state.deliverableIds.clear();
        renderDeliverableOptions();
        reloadEntries();
    });
    fromInput.addEventListener('change', () => { state.from = fromInput.value; refresh(); });
    toInput.addEventListener('change', () => { state.to = toInput.value; refresh(); });
    userSelect.addEventListener('change', () => { state.userEmail = userSelect.value; refresh(); });

    lockButton.addEventListener('click', async () => {
        const count = data.rows.filter(row => !row.entry.locked).length;
        if (!count) {
            statusLine.textContent = t('kpi.nothingToLock', 'Alle Zeiteinträge im Umfang sind bereits gesperrt.');
            return;
        }
        if (!window.confirm(t('kpi.lockConfirm', '{count} Zeiteinträge im Zeitraum {period} sperren? Gesperrte Einträge können nicht mehr bearbeitet oder gelöscht werden.', { count, period: periodLabel() })))
            return;
        lockButton.disabled = true;
        try {
            const scope = collectReportData({ hierarchy, tasks, entriesByTask }, state);
            const userId = state.userEmail
                ? data.rows.find(row => row.entry.userEmail === state.userEmail)?.entry.userId || null
                : null;
            const response = await fetch('/api/time-entries/lock-period', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    taskIds: scope.scopedTasks.map(task => task.id),
                    from: `${state.from}T00:00:00Z`,
                    to: `${state.to}T23:59:59Z`,
                    userId,
                    lockedPeriod: periodLabel()
                })
            });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const result = await response.json();
            context.clearEntryCache?.();
            await reloadEntries();
            context.onLocksChanged?.();
            statusLine.textContent = t('kpi.lockSuccess', '{count} Zeiteinträge gesperrt.', { count: result.lockedCount });
        } catch (error) {
            console.error('Lock period failed:', error);
            statusLine.textContent = t('kpi.lockFailed', 'Sperren fehlgeschlagen.');
        }
        lockButton.disabled = false;
    });

    excelButton.addEventListener('click', async () => {
        excelButton.disabled = true;
        try {
            const response = await fetch('/api/v1/billing-report/export', {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    title: t('kpi.billingReport', 'Abrechnungsreport'),
                    metadata: metadataPairs(),
                    summary: summaryPairs(),
                    headers: entryHeaders(),
                    rows: entryRows()
                })
            });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            downloadBlob(await response.blob(), `abrechnungsreport_${state.from}_${state.to}.xlsx`);
        } catch (error) {
            console.error('Excel export failed:', error);
            statusLine.textContent = t('kpi.exportFailed', 'Export fehlgeschlagen.');
        }
        excelButton.disabled = false;
    });

    pdfButton.addEventListener('click', async () => {
        pdfButton.disabled = true;
        try {
            const JsPdf = await ensureJsPdf();
            const doc = new JsPdf({ unit: 'mm', format: 'a4' });
            const pageWidth = doc.internal.pageSize.getWidth();
            const margin = 14;

            // header: logo top-right with graceful fallback
            if (state.logo) {
                const dims = await loadImageDimensions(state.logo);
                if (dims && dims.width > 0) {
                    const width = 35;
                    const height = Math.min(25, width * dims.height / dims.width);
                    try { doc.addImage(state.logo, 'PNG', pageWidth - margin - width, margin, width, height); } catch { /* unsupported image */ }
                }
            }
            doc.setFontSize(16);
            doc.setFont('helvetica', 'bold');
            doc.text(t('kpi.billingReport', 'Abrechnungsreport'), margin, margin + 6);
            doc.setFontSize(10);
            doc.setFont('helvetica', 'normal');
            let y = margin + 16;
            for (const [label, value] of metadataPairs()) {
                doc.setFont('helvetica', 'bold');
                doc.text(`${label}:`, margin, y);
                doc.setFont('helvetica', 'normal');
                doc.text(String(value), margin + 48, y);
                y += 5.5;
            }

            // page 1: executive summary with totals only
            doc.setFontSize(12);
            doc.setFont('helvetica', 'bold');
            doc.text(t('kpi.executiveSummary', 'Kurzfassung'), margin, y + 6);
            doc.autoTable({
                startY: y + 9,
                margin: { left: margin, right: margin },
                head: [[t('kpi.metric', 'Kennzahl'), t('kpi.value', 'Wert')]],
                body: summaryPairs(),
                styles: { fontSize: 9 },
                headStyles: { fillColor: [30, 41, 59] }
            });
            const perDeliverable = data.selectedDeliverables.map(item => {
                const rows = data.rows.filter(row => row.task.deliverableId === item.id);
                const hours = rows.reduce((sum, row) => sum + toNumber(row.entry.durationHours), 0);
                const billable = rows.reduce((sum, row) => sum + (row.entry.isBillable ? toNumber(row.entry.durationHours) : 0), 0);
                return [item.name, formatHours(toNumber(item.budgetHours) > 0 ? toNumber(item.budgetHours) : null), formatHours(hours), formatHours(billable)];
            }).filter(row => row[2] !== '0h');
            if (perDeliverable.length) {
                doc.autoTable({
                    startY: doc.lastAutoTable.finalY + 6,
                    margin: { left: margin, right: margin },
                    head: [[t('hierarchy.deliverable', 'Lieferobjekt'), t('kpi.budgetShort', 'Budget'), t('kpi.actualShort', 'Ist'), t('kpi.billable', 'Abrechenbar')]],
                    body: perDeliverable,
                    styles: { fontSize: 9 },
                    headStyles: { fillColor: [30, 41, 59] }
                });
            }

            // details from page 2 onwards
            doc.addPage();
            doc.setFontSize(12);
            doc.setFont('helvetica', 'bold');
            doc.text(t('kpi.entryDetails', 'Zeiteinträge im Detail'), margin, margin + 4);
            doc.autoTable({
                startY: margin + 8,
                margin: { left: margin, right: margin },
                head: [entryHeaders()],
                body: entryRows(),
                styles: { fontSize: 8 },
                headStyles: { fillColor: [30, 41, 59] }
            });

            const pageCount = doc.internal.getNumberOfPages();
            for (let page = 1; page <= pageCount; page++) {
                doc.setPage(page);
                doc.setFontSize(8);
                doc.setFont('helvetica', 'normal');
                doc.text(`${page} / ${pageCount}`, pageWidth - margin, doc.internal.pageSize.getHeight() - 8, { align: 'right' });
            }
            doc.save(`abrechnungsreport_${state.from}_${state.to}.pdf`);
        } catch (error) {
            console.error('PDF export failed:', error);
            statusLine.textContent = t('kpi.exportFailed', 'Export fehlgeschlagen.');
        }
        pdfButton.disabled = false;
    });

    renderCustomerOptions();
    renderProjectOptions();
    renderDeliverableOptions();
    refresh();
    reloadEntries();
    close.focus();
}

export { formatHours, formatPercent, isoWeek };
