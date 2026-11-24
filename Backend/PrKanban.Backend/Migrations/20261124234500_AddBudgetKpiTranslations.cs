using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261124234500_AddBudgetKpiTranslations")]
public sealed class AddBudgetKpiTranslations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('common.yes', 'Allgemein', 'Ja', 'Yes'),
                ('common.no', 'Allgemein', 'Nein', 'No'),
                ('kpi.budgetMetrics', 'Dashboard', 'Budget-Kennzahlen', 'Budget metrics'),
                ('kpi.budgetBurn', 'Dashboard', 'Budget Burn', 'Budget burn'),
                ('kpi.budgetBurnExplanation', 'Dashboard', 'Erfasste Ist-Stunden im Verhältnis zum zugewiesenen Budget (Summe der Lieferobjekt-Budgets im Filter).', 'Logged actual hours relative to the assigned budget (sum of the deliverable budgets in the current filter).'),
                ('kpi.remainingBudget', 'Dashboard', 'Restbudget', 'Remaining budget'),
                ('kpi.remainingBudgetExplanation', 'Dashboard', 'Zugewiesenes Budget abzüglich erfasster Ist-Stunden. Negative Werte zeigen eine Überschreitung.', 'Assigned budget minus logged actual hours. Negative values indicate an overrun.'),
                ('kpi.remainingBudgetNote', 'Dashboard', 'Budget − Ist', 'Budget − actual'),
                ('kpi.unallocatedBudget', 'Dashboard', 'Nicht allokiertes Budget', 'Unallocated budget'),
                ('kpi.unallocatedExplanation', 'Dashboard', 'Lieferobjekt-Budgets abzüglich der auf Tasks verteilten Budgets. Negative Werte bedeuten Überallokation.', 'Deliverable budgets minus the budgets allocated to tasks. Negative values indicate over-allocation.'),
                ('kpi.billableShare', 'Dashboard', 'Abrechenbarer Anteil', 'Billable share'),
                ('kpi.billableShareExplanation', 'Dashboard', 'Anteil der abrechenbaren Stunden an allen erfassten Stunden (unabhängig vom Abrechenbar-Filter).', 'Share of billable hours in all logged hours (independent of the billable filter).'),
                ('kpi.budgetBreakdown', 'Dashboard', 'Budgetaufschlüsselung', 'Budget breakdown'),
                ('kpi.budgetBreakdownNote', 'Dashboard', 'Budget, Schätzung und Ist-Stunden je Kunde, Projekt, Lieferobjekt und Task. Balken zeigen abrechenbare und nicht abrechenbare Anteile relativ zum Budget (oder zur Schätzung).', 'Budget, estimate and actual hours per customer, project, deliverable and task. Bars show billable and non-billable shares relative to the budget (or estimate).'),
                ('kpi.budgetCharts', 'Dashboard', 'Verlaufsdiagramme', 'Historical charts'),
                ('kpi.cumulativeBurn', 'Dashboard', 'Kumulierter Budgetverbrauch', 'Cumulative budget burn'),
                ('kpi.cumulativeBurnExplanation', 'Dashboard', 'Kumulierte Ist-Stunden im Zeitverlauf gegenüber Budget- und Schätzungslinien.', 'Cumulative actual hours over time against budget and estimate reference lines.'),
                ('kpi.weeklyBurn', 'Dashboard', 'Wöchentliche Burn Rate', 'Weekly burn rate'),
                ('kpi.weeklyBurnExplanation', 'Dashboard', 'Erfasste Stunden je Kalenderwoche, aufgeteilt nach abrechenbar und nicht abrechenbar.', 'Logged hours per calendar week, split into billable and non-billable.'),
                ('kpi.riskRanking', 'Dashboard', 'Überschreitungs-Ranking', 'Overrun ranking'),
                ('kpi.riskRankingExplanation', 'Dashboard', 'Tasks mit der höchsten Auslastung ihres Budgets bzw. ihrer Schätzung. Rot markierte Balken überschreiten die Referenz.', 'Tasks with the highest utilisation of their budget or estimate. Red bars exceed the reference.'),
                ('kpi.riskNote', 'Dashboard', 'Aufgaben ohne Budget und Schätzung werden nicht bewertet.', 'Tasks without a budget or estimate are not ranked.'),
                ('kpi.estimateScatter', 'Dashboard', 'Schätzung vs. Ist', 'Estimate vs. actual'),
                ('kpi.estimateScatterExplanation', 'Dashboard', 'Jede Aufgabe als Punkt: x = Schätzung, y = Ist-Stunden. Punkte über der Diagonalen wurden unterschätzt.', 'Each task as a point: x = estimate, y = actual hours. Points above the diagonal were underestimated.'),
                ('kpi.parityLine', 'Dashboard', 'Ist = Schätzung', 'Actual = estimate'),
                ('kpi.budgetShort', 'Dashboard', 'Budget', 'Budget'),
                ('kpi.estimateShort', 'Dashboard', 'Schätzung', 'Estimate'),
                ('kpi.actualShort', 'Dashboard', 'Ist', 'Actual'),
                ('kpi.billable', 'Dashboard', 'Abrechenbar', 'Billable'),
                ('kpi.nonBillable', 'Dashboard', 'Nicht abrechenbar', 'Non-billable'),
                ('kpi.overrun', 'Dashboard', 'Überschreitung', 'Overrun'),
                ('kpi.billableOnly', 'Dashboard', 'Nur abrechenbare Stunden', 'Billable hours only'),
                ('kpi.billingAssistant', 'Dashboard', 'Abrechnungsreport-Assistent', 'Billing Report Assistant'),
                ('kpi.billingAssistantIntro', 'Dashboard', 'Erstellt demnächst Abrechnungsreports auf Basis der aktiven Filter. Vorab-Prüfungen, KI-Zusammenfassung und Export folgen.', 'Will soon create billing reports based on the active filters. Pre-flight checks, AI summary and export follow.'),
                ('kpi.activeFilters', 'Dashboard', 'Aktive Filter', 'Active filters'),
                ('kpi.reportScope', 'Dashboard', 'Report-Umfang', 'Report scope'),
                ('kpi.assistantStepChecks', 'Dashboard', 'Vorab-Prüfungen (fehlende Kommentare, nicht klassifizierte Einträge)', 'Pre-flight checks (missing comments, unclassified entries)'),
                ('kpi.assistantStepSummary', 'Dashboard', 'KI-Zusammenfassung der erbrachten Leistungen', 'AI summary of the delivered work'),
                ('kpi.assistantStepExport', 'Dashboard', 'Export als PDF/Excel', 'Export as PDF/Excel'),
                ('kpi.budgetCoverage', 'Dashboard', 'Auswertung nach aktuellen Filtern. Budgets stammen von den Lieferobjekten der gefilterten Aufgaben; Ist-Stunden aus den Zeiteinträgen der Aufgaben.', 'Based on current filters. Budgets come from the deliverables of the filtered tasks; actual hours from the tasks'' time entries.'),
                ('kpi.filterAll', 'Dashboard', 'Alle', 'All');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM "AppTranslations" WHERE "Key" IN (
                'common.yes', 'common.no',
                'kpi.budgetMetrics', 'kpi.budgetBurn', 'kpi.budgetBurnExplanation',
                'kpi.remainingBudget', 'kpi.remainingBudgetExplanation', 'kpi.remainingBudgetNote',
                'kpi.unallocatedBudget', 'kpi.unallocatedExplanation',
                'kpi.billableShare', 'kpi.billableShareExplanation',
                'kpi.budgetBreakdown', 'kpi.budgetBreakdownNote', 'kpi.budgetCharts',
                'kpi.cumulativeBurn', 'kpi.cumulativeBurnExplanation',
                'kpi.weeklyBurn', 'kpi.weeklyBurnExplanation',
                'kpi.riskRanking', 'kpi.riskRankingExplanation', 'kpi.riskNote',
                'kpi.estimateScatter', 'kpi.estimateScatterExplanation', 'kpi.parityLine',
                'kpi.budgetShort', 'kpi.estimateShort', 'kpi.actualShort',
                'kpi.billable', 'kpi.nonBillable', 'kpi.overrun', 'kpi.billableOnly',
                'kpi.billingAssistant', 'kpi.billingAssistantIntro', 'kpi.activeFilters', 'kpi.reportScope',
                'kpi.assistantStepChecks', 'kpi.assistantStepSummary', 'kpi.assistantStepExport',
                'kpi.budgetCoverage', 'kpi.filterAll');
            """);
    }
}
