import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
import assert from 'node:assert/strict';

const source = await readFile(new URL('../wwwroot/task-table.js', import.meta.url), 'utf8');
const { groupTasksForTable, filterTasksForTable, compareTasksForTable, tableExportRows, executeTableBulk, projectStatusTargets, partitionTasksByProject, hierarchyBulkTargets } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);

test('groups master-board tasks by status in column order and keeps unknown statuses', () => {
    const tasks = [
        { id: '1', column: 'Done' },
        { id: '2', column: 'ToDo' },
        { id: '3', column: 'Custom' }
    ];
    const groups = groupTasksForTable(tasks, [
        { key: 'ToDo', label: 'To do' },
        { key: 'Done', label: 'Done' },
        { key: 'Empty', label: 'Empty' }
    ], false, key => key, 'Unassigned');
    assert.deepEqual(groups.map(group => [group.label, group.tasks.map(task => task.id)]), [
        ['To do', ['2']], ['Done', ['1']], ['Custom', ['3']]
    ]);
});

test('uses project columns and retains tasks without a project position', () => {
    const tasks = [
        { id: '1', column: 'Done', projectBoardColumnId: 'planning' },
        { id: '2', column: 'ToDo', projectBoardColumnId: null }
    ];
    const groups = groupTasksForTable(tasks, [{ key: 'planning', label: 'Planning' }],
        true, key => key, 'Unassigned');
    assert.deepEqual(groups.map(group => [group.label, group.tasks.map(task => task.id)]), [
        ['Planning', ['1']], ['Unassigned', ['2']]
    ]);
});

test('groups by assignee and type without losing unassigned tasks', () => {
    const tasks = [
        { id: '1', owner: 'Zoe', taskType: 'Bug' },
        { id: '2', owner: 'Anna', taskType: 'Task' },
        { id: '3', owner: null, taskType: 'Bug' }
    ];
    const group = field => groupTasksForTable(tasks, [], false, key => key, 'Unassigned', field);
    assert.deepEqual(group('owner').map(item => [item.label, item.tasks.map(task => task.id)]), [
        ['Anna', ['2']], ['Unassigned', ['3']], ['Zoe', ['1']]
    ]);
    assert.deepEqual(group('taskType').map(item => [item.label, item.tasks.map(task => task.id)]), [
        ['Bug', ['1', '3']], ['Task', ['2']]
    ]);
});

test('groups projects and deliverables by ID even when names are identical', () => {
    const tasks = [
        { id: '1', projectId: 'p1', projectName: 'Project', deliverableId: 'd1', deliverableName: 'Item' },
        { id: '2', projectId: 'p2', projectName: 'Project', deliverableId: 'd2', deliverableName: 'Item' },
        { id: '3' }
    ];
    for (const field of ['project', 'deliverable']) {
        const groups = groupTasksForTable(tasks, [], false, key => key, 'Unassigned', field);
        assert.deepEqual(groups.map(item => item.tasks.map(task => task.id)), [['1'], ['2'], ['3']]);
    }
});

test('ungrouped mode retains all tasks without creating empty groups', () => {
    const tasks = [{ id: '1' }, { id: '2' }];
    const group = items => groupTasksForTable(items, [], false, key => key, 'Unassigned', 'none');
    assert.deepEqual(group(tasks), [{ key: null, label: '', tasks }]);
    assert.deepEqual(group([]), []);
    assert.notEqual(group(tasks)[0].tasks, tasks);
});

test('nested groups keep project identities and export in subgroup order', () => {
    const tasks = [
        { id: '1', column: 'ToDo', projectId: 'p1', projectName: 'Same' },
        { id: '2', column: 'Done', projectId: 'p1', projectName: 'Same' },
        { id: '3', column: 'ToDo', projectId: 'p2', projectName: 'Same' }
    ];
    const columns = [{ key: 'ToDo', label: 'To do' }, { key: 'Done', label: 'Done' }];
    const group = (items, field) => groupTasksForTable(items, columns, false, key => key, 'Unassigned', field);
    const subgroups = group(tasks, 'status').flatMap(parent => group(parent.tasks, 'project'));
    assert.deepEqual(subgroups.map(item => [item.key, item.tasks.map(task => task.id)]), [
        ['p1', ['1']], ['p2', ['3']], ['p1', ['2']]
    ]);
    assert.deepEqual(tableExportRows(subgroups, [{ key: 'id' }], task => task), [['1'], ['3'], ['2']]);
});

test('combines case-insensitive filters across columns and permits clearing them', () => {
    const tasks = [
        { id: '1', title: 'Übersicht', owner: 'Anna' },
        { id: '2', title: 'Übersicht prüfen', owner: 'Ben' },
        { id: '3', title: 'Andere', owner: 'Anna' }
    ];
    const values = task => task;
    assert.deepEqual(filterTasksForTable(tasks, { title: ' ÜBER ', owner: 'anna' }, values).map(task => task.id), ['1']);
    assert.deepEqual(filterTasksForTable(tasks, { title: 'missing' }, values), []);
    assert.deepEqual(filterTasksForTable(tasks, { title: '', owner: '  ' }, values), tasks);
});

test('sorts naturally in both directions and preserves the existing order for ties', () => {
    const tasks = [
        { id: '1', taskNumber: 'PRJ-10', order: 0 },
        { id: '2', taskNumber: 'PRJ-2', order: 1 },
        { id: '3', taskNumber: 'prj-2', order: 2 }
    ];
    const values = task => task;
    const fallback = (a, b) => a.order - b.order;
    const sorted = direction => [...tasks].sort((a, b) =>
        compareTasksForTable(a, b, { key: 'taskNumber', direction }, values, fallback)).map(task => task.id);
    assert.deepEqual(sorted('asc'), ['2', '3', '1']);
    assert.deepEqual(sorted('desc'), ['1', '2', '3']);
    assert.deepEqual([...tasks].reverse().sort((a, b) =>
        compareTasksForTable(a, b, null, values, fallback)).map(task => task.id), ['1', '2', '3']);
});

test('export rows preserve grouped order and requested column order, including empty values', () => {
    const groups = [
        { tasks: [{ id: 'P-2', title: 'Second', tags: 'Backend, Urgent' }] },
        { tasks: [{ id: 'P-1', title: '=literal', tags: '—' }] }
    ];
    const columns = [{ key: 'id' }, { key: 'title' }, { key: 'tags' }];
    assert.deepEqual(tableExportRows(groups, columns, task => task), [
        ['P-2', 'Second', 'Backend, Urgent'], ['P-1', '=literal', '—']
    ]);
    assert.deepEqual(tableExportRows([], columns, task => task), []);
});

test('bulk actions run sequentially and retain individual failures and unchanged items', async () => {
    const calls = [];
    const result = await executeTableBulk(['a', 'b', 'c'], async item => {
        calls.push(`start-${item}`);
        await Promise.resolve();
        calls.push(`end-${item}`);
        if (item === 'b') throw new Error('denied');
        return item !== 'c';
    });

    assert.deepEqual(calls, ['start-a', 'end-a', 'start-b', 'end-b', 'start-c', 'end-c']);
    assert.deepEqual(result.completed, [{ item: 'a', changed: true }, { item: 'c', changed: false }]);
    assert.equal(result.failed[0].item, 'b');
    assert.equal(result.failed[0].error.message, 'denied');
});

test('status targets retain project and column identity even when names match', () => {
    const targets = projectStatusTargets([
        { projectId: 'p1', columns: [{ id: 'c1', name: 'Analysis', masterColumnName: 'Analysis', showSubLanes: true }] },
        { projectId: 'p2', columns: [{ id: 'c2', name: 'Analysis', masterColumnName: 'ToDo', showSubLanes: false }] }
    ]);
    assert.deepEqual(targets.map(({ projectId, projectColumnId, column, subColumn }) =>
        [projectId, projectColumnId, column, subColumn]), [
        ['p1', 'c1', 'Analysis', 'Doing'], ['p1', 'c1', 'Analysis', 'Done'], ['p2', 'c2', 'ToDo', null]
    ]);
});

test('a project status affects only selected tasks from that project', () => {
    const selection = [
        { id: '1', projectId: 'p1' }, { id: '2', projectId: 'p2' },
        { id: '3', projectId: 'p1' }, { id: '4', projectId: null }
    ];
    const { included, excluded } = partitionTasksByProject(selection, 'p1');
    assert.deepEqual(included.map(task => task.id), ['1', '3']);
    assert.deepEqual(excluded.map(task => task.id), ['2', '4']);
});

test('destination targets preserve project, customer and deliverable identity for duplicate names', () => {
    const targets = hierarchyBulkTargets({
        customers: [{ id: 'c1', name: 'Customer A' }, { id: 'c2', name: 'Customer B' }],
        projects: [
            { id: 'p1', name: 'Project', shortKey: 'P1', customerId: 'c1' },
            { id: 'p2', name: 'Project', shortKey: 'P2', customerId: 'c2' },
            { id: 'p3', name: 'Empty', customerId: 'c1' }
        ],
        deliverables: [
            { id: 'd1', name: 'Delivery', projectId: 'p1' },
            { id: 'd2', name: 'Delivery', projectId: 'p2' }
        ]
    });
    assert.deepEqual(targets.map(project => [project.id, project.customerName, project.deliverables.map(item => item.id)]), [
        ['p1', 'Customer A', ['d1']], ['p2', 'Customer B', ['d2']], ['p3', 'Customer A', []]
    ]);
});
