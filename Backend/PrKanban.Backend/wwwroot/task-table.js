export function groupTasksForTable(tasks, columns, isProject, getColumnLabel, unassignedLabel, grouping = 'status') {
    if (grouping === 'none') return tasks.length ? [{ key: null, label: '', tasks: [...tasks] }] : [];
    const groups = grouping === 'status'
        ? new Map(columns.map(column => [column.key, { ...column, tasks: [] }]))
        : new Map();
    for (const task of tasks) {
        let key;
        let label;
        if (grouping === 'status') {
            key = isProject ? task.projectBoardColumnId : task.column;
            label = isProject ? unassignedLabel : getColumnLabel(key);
        } else if (grouping === 'project') {
            key = task.projectId || null;
            label = task.projectName || unassignedLabel;
        } else if (grouping === 'deliverable') {
            key = task.deliverableId || null;
            label = task.deliverableName || unassignedLabel;
        } else {
            key = task[grouping] || null;
            label = key || unassignedLabel;
        }
        if (!groups.has(key)) {
            groups.set(key, { key, label, tasks: [] });
        }
        groups.get(key).tasks.push(task);
    }
    const result = [...groups.values()].filter(group => group.tasks.length);
    if (grouping !== 'status') result.sort((a, b) => a.label.localeCompare(b.label));
    return result;
}

const tableCollator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });

export function filterTasksForTable(tasks, filters, getValues) {
    const activeFilters = Object.entries(filters)
        .map(([key, value]) => [key, value.trim().toLocaleLowerCase()])
        .filter(([, value]) => value);
    if (!activeFilters.length) return tasks;
    return tasks.filter(task => {
        const values = getValues(task);
        return activeFilters.every(([key, value]) => values[key].toLocaleLowerCase().includes(value));
    });
}

export function compareTasksForTable(a, b, sort, getValues, fallback) {
    if (!sort) return fallback(a, b);
    const comparison = tableCollator.compare(getValues(a)[sort.key], getValues(b)[sort.key]);
    return (sort.direction === 'desc' ? -comparison : comparison) || fallback(a, b);
}

export function tableExportRows(groups, columns, getValues) {
    return groups.flatMap(group => group.tasks.map(task => {
        const values = getValues(task);
        return columns.map(column => values[column.key]);
    }));
}

export async function executeTableBulk(items, action) {
    const completed = [];
    const failed = [];
    for (const item of items) {
        try {
            const changed = await action(item);
            completed.push({ item, changed });
        } catch (error) {
            failed.push({ item, error });
        }
    }
    return { completed, failed };
}

export function projectStatusTargets(boards) {
    return boards.flatMap(board => board.columns.flatMap(column =>
        (column.showSubLanes ? ['Doing', 'Done'] : [null]).map(subColumn => ({
            projectId: board.projectId,
            projectBoard: board,
            projectColumnId: column.id,
            column: column.masterColumnName,
            columnLabel: column.name,
            subColumn
        }))));
}

export function partitionTasksByProject(tasks, projectId) {
    const included = [];
    const excluded = [];
    for (const task of tasks) {
        (task.projectId === projectId ? included : excluded).push(task);
    }
    return { included, excluded };
}

export function hierarchyBulkTargets(hierarchy) {
    const customers = new Map(hierarchy.customers.map(customer => [customer.id, customer.name]));
    return hierarchy.projects.map(project => ({
        id: project.id,
        name: project.name,
        shortKey: project.shortKey,
        customerName: customers.get(project.customerId),
        deliverables: hierarchy.deliverables.filter(deliverable => deliverable.projectId === project.id)
            .map(deliverable => ({ id: deliverable.id, name: deliverable.name }))
    }));
}
