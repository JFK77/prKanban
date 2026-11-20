using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PrKanban.Data;

namespace PrKanban.Migrations;

[DbContext(typeof(PrKanbanContext))]
[Migration("20261120000900_AddKanbanKpiExplanations")]
public sealed class AddKanbanKpiExplanations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "AppTranslations" ("Key", "Category", "German", "English") VALUES
                ('kpi.wipExplanation', 'Dashboard', 'Die Anzahl der Aufgaben, die sich aktuell gleichzeitig in Bearbeitung befinden. Ein hoher WIP führt oft zu Kontextwechseln und längeren Durchlaufzeiten (Little’s Law).', 'The number of tasks currently in progress at the same time. High WIP often leads to context switching and longer cycle times (Little’s Law).'),
                ('kpi.cycleTimeExplanation', 'Dashboard', 'Die Zeit, die eine Aufgabe benötigt, ab dem Moment, in dem aktiv daran gearbeitet wird (In Progress), bis zur Fertigstellung (Done). Sie misst die reine Arbeitsgeschwindigkeit des Teams.', 'The time a task takes from when work actively begins (In Progress) until completion (Done). It measures the working speed of the team.'),
                ('kpi.leadTimeExplanation', 'Dashboard', 'Die gesamte Zeit von der Erstellung eines Tickets (Kundenwunsch im Backlog) bis zur Auslieferung (Done). Sie spiegelt die Wartezeit aus Kundensicht wider.', 'The total time from creation of a ticket (customer request in the backlog) until delivery (Done). It reflects waiting time from the customer perspective.'),
                ('kpi.throughputExplanation', 'Dashboard', 'Die Anzahl der abgeschlossenen Aufgaben innerhalb eines definierten Zeitraums (z. B. Tickets pro Woche). Dient als Basis für Kapazitätsprognosen.', 'The number of tasks completed within a defined period (e.g. tickets per week). It provides a basis for capacity forecasts.'),
                ('kpi.cfdExplanation', 'Dashboard', 'Zeigt die Verteilung der Aufgaben über alle Board-Spalten im Zeitverlauf. Laufend breiter werdende Bänder weisen direkt auf einen Engpass (Bottleneck) hin.', 'Shows the distribution of tasks across all board columns over time. Continuously widening bands indicate a bottleneck.'),
                ('kpi.controlChartExplanation', 'Dashboard', 'Stellt die Cycle Time jeder einzelnen Aufgabe als Punkt dar. Hilft dabei, Ausreißer (ungewöhnlich lange Tasks) zu identifizieren und die Prozessstabilität zu bewerten.', 'Plots the cycle time of each task as a point. It helps identify outliers (unusually long tasks) and assess process stability.'),
                ('kpi.scatterplotExplanation', 'Dashboard', 'Ähnlich wie das Control Chart, ergänzt um Perzentil-Linien (z. B. 85 %). Damit lassen sich verlässliche Aussagen treffen (z. B. "85 % aller Aufgaben erledigen wir in unter 6 Tagen").', 'Similar to the control chart, with percentile lines (e.g. 85%). This enables reliable statements (e.g. "85% of tasks are completed in under 6 days").'),
                ('kpi.agingWipExplanation', 'Dashboard', 'Zeigt an, wie lange sich aktuell laufende Aufgaben bereits in einer Spalte befinden. Warnt proaktiv vor Aufgaben, die liegen bleiben, noch bevor sie abgeschlossen sind.', 'Shows how long tasks currently in progress have spent in a column. It proactively warns about tasks that are stalled before they are completed.');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
