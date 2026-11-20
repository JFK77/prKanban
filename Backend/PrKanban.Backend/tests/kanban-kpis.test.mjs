import { readFile } from 'node:fs/promises';
import { test } from 'node:test';
import assert from 'node:assert/strict';

const source = await readFile(new URL('../wwwroot/kanban-kpis.js', import.meta.url), 'utf8');
const { calculateKanbanKpis, renderFlowChart } = await import(`data:text/javascript;base64,${Buffer.from(source).toString('base64')}`);
const now = Date.parse('2026-11-20T12:00:00Z');
const event = (id, columnName, occurredAt, isBaseline = false) => ({ id, columnName, occurredAt, isBaseline });
const task = (id, history, createdAt = '2026-11-01T00:00:00Z') => ({ id, createdAt, statusHistory: history });

test('calculates recorded completions, current ages and daily state', () => {
    const data = calculateKanbanKpis([
        task('one', [
            event(1, 'Backlog', '2026-11-01T00:00:00Z'),
            event(2, 'InProgress', '2026-11-04T00:00:00Z'),
            event(3, 'InProgress', '2026-11-05T00:00:00Z'),
            event(4, 'Done', '2026-11-07T00:00:00Z')
        ]),
        task('two', [
            event(5, 'ToDo', '2026-11-10T00:00:00Z'),
            event(6, 'InProgress', '2026-11-18T12:00:00Z'),
            event(7, 'InProgress', '2026-11-19T12:00:00Z')
        ])
    ], now);
    assert.equal(data.cycle, 3);
    assert.equal(data.lead, 6);
    assert.equal(data.throughput, 1);
    assert.equal(data.aging[0].days, 2);
    assert.equal(data.daily.find(day => day.date === Date.parse('2026-11-05T00:00:00Z')).counts.InProgress, 1);
    assert.equal(data.daily.at(-1).counts.Done, 1);
});

test('does not infer legacy cycle time or age from a baseline', () => {
    const data = calculateKanbanKpis([
        task('done', [event(1, 'InProgress', '2026-11-10T00:00:00Z', true), event(2, 'Done', '2026-11-12T00:00:00Z')]),
        task('active', [event(3, 'Analysis', '2026-11-10T00:00:00Z', true)])
    ], now);
    assert.equal(data.throughput, 1);
    assert.equal(data.cycle, null);
    assert.equal(data.lead, 11);
    assert.equal(data.aging[0].days, null);
});

test('counts each observed completion after reopening and resets cycle start', () => {
    const data = calculateKanbanKpis([task('reopened', [
        event(1, 'Backlog', '2026-11-01T00:00:00Z'),
        event(2, 'InProgress', '2026-11-02T00:00:00Z'),
        event(3, 'Done', '2026-11-04T00:00:00Z'),
        event(4, 'ToDo', '2026-11-05T00:00:00Z'),
        event(5, 'InProgress', '2026-11-06T00:00:00Z'),
        event(6, 'Done', '2026-11-10T00:00:00Z')
    ])], now);
    assert.equal(data.throughput, 2);
    assert.equal(data.cycle, 3);
    assert.deepEqual(data.completions.map(item => item.cycle), [2, 4]);
});

test('excludes completions outside the UTC window and honors supplied task subset', () => {
    const old = task('old', [event(1, 'InProgress', '2026-10-10T00:00:00Z'), event(2, 'Done', '2026-10-20T00:00:00Z')], '2026-10-01T00:00:00Z');
    const recent = task('recent', [event(3, 'InProgress', '2026-11-01T00:00:00Z'), event(4, 'Done', '2026-11-02T00:00:00Z')]);
    assert.equal(calculateKanbanKpis([old, recent], now).throughput, 1);
    assert.equal(calculateKanbanKpis([old], now).throughput, 0);
});

test('empty charts show the centered, translated no-data message', () => {
    const previousDocument = globalThis.document;
    globalThis.document = {
        createElement: tag => ({
            tag, children: [],
            append(...children) { this.children.push(...children); }
        })
    };
    try {
        const data = calculateKanbanKpis([], now);
        const message = 'No data is available for this period yet. Please check filters as well.';
        const translate = key => key === 'kpi.noDataForPeriod' ? message : key;
        for (const kind of ['cfd', 'control', 'aging', 'scatter']) {
            const note = renderFlowChart(kind, data, translate).children[0];
            assert.equal(note.textContent, message);
            assert.match(note.className, /\bitems-center\b/);
            assert.match(note.className, /\bjustify-center\b/);
            assert.match(note.className, /\btext-center\b/);
        }
    } finally {
        globalThis.document = previousDocument;
    }
});

test('individual chart points and aging bars open their own task', () => {
    class Element {
        constructor(tag) { this.tag = tag; this.children = []; this.attributes = {}; this.listeners = {}; this.style = {}; }
        append(...children) { this.children.push(...children); }
        replaceChildren(...children) { this.children = children; }
        setAttribute(name, value) { this.attributes[name] = value; }
        addEventListener(name, callback) { this.listeners[name] = callback; }
        querySelector(tag) { return this.children.find(child => child.tag === tag); }
        focus() { this.focused = true; }
    }
    const previousDocument = globalThis.document;
    globalThis.document = {
        createElement: tag => new Element(tag),
        createElementNS: (_namespace, tag) => new Element(tag)
    };
    try {
        const selected = [];
        const completed = task('done', [event(1, 'InProgress', '2026-11-02T00:00:00Z'), event(2, 'Done', '2026-11-04T00:00:00Z')]);
        const active = task('active', [event(3, 'InProgress', '2026-11-18T00:00:00Z')]);
        const data = calculateKanbanKpis([completed, active], now);
        const translate = (_key, fallback, values = {}) =>
            fallback.replace(/\{(\w+)\}/g, (_match, key) => values[key]);
        for (const kind of ['control', 'scatter']) {
            const chart = renderFlowChart(kind, data, translate, column => column, id => selected.push(id));
            const point = chart.children[0].children.find(child => child.tag === 'circle');
            assert.equal(point.attributes.role, 'button');
            assert.equal(point.attributes.tabindex, '0');
            point.listeners.click();
            let prevented = false;
            point.listeners.keydown({ key: ' ', preventDefault: () => { prevented = true; } });
            point.listeners.keydown({ key: 'Escape', preventDefault: () => assert.fail('Unexpected key handled') });
            assert.equal(prevented, true);
        }
        const chart = renderFlowChart('aging', data, translate, column => column, id => selected.push(id));
        const barButton = chart.children[0].children[0];
        assert.equal(barButton.tag, 'button');
        barButton.listeners.click();
        assert.deepEqual(selected, ['done', 'done', 'done', 'done', 'active']);

        const duplicate = task('other', [event(4, 'InProgress', '2026-11-02T00:00:00Z'), event(5, 'Done', '2026-11-04T00:00:00Z')]);
        const overlap = renderFlowChart('scatter', calculateKanbanKpis([completed, duplicate], now),
            translate, column => column, id => selected.push(id));
        const points = overlap.children[0].children.filter(child => child.tag === 'circle');
        assert.equal(points.length, 1);
        points[0].listeners.click();
        const choices = overlap.children[1].children.filter(child => child.tag === 'button');
        assert.equal(choices.length, 2);
        assert.equal(choices[0].focused, true);
        choices[1].listeners.click();
        assert.equal(selected.at(-1), 'other');
    } finally {
        globalThis.document = previousDocument;
    }
});
