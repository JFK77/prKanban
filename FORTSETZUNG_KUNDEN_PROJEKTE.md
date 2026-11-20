# Fortsetzung: Projekte, Lieferobjekte und Projektboards

Stand: 17. November 2026

## Aktueller Stand

- Administration mit Benutzerverwaltung, Übersetzungstabelle und schreibgeschützten Datenbankinformationen ist vorhanden.
- Die Sprachwahl DE/EN befindet sich im Board vor dem Theme-Schalter.
- Task-Beschreibungen unterstützen Formatierung, Links, eingefügte Bilder mit Größenänderung und Dateianhänge.
- Admins und Users können alle Tasks bearbeiten und verschieben; „Zugewiesen an“ bestimmt keine Zugriffsrechte.
- Die Reihenfolge von Tasks und das Verschieben zwischen Spalten werden gespeichert.
- Das Task-Panel hält Schließen und Speichern sichtbar; die Dropdowns für zugewiesene Personen und Kunden wurden verbessert.
- Letzter gepushter Commit: `b8acb30` auf `feature/migrate-firebase-to-sqlite`.

## Nächster Schwerpunkt

Die beiden zusammenhängenden Ideen vorbereiten: eine Struktur für **Kunde → Projekt → Lieferobjekt → Task** und projektbezogene Kanbanboards.

### Kunden, Projekte und Zeitbudgets

- Ein Kunde hat mehrere Projekte, ein Projekt mehrere Lieferobjekte und ein Lieferobjekt mehrere Tasks.
- Jeder Task gehört zu genau einem Lieferobjekt.
- Jedes Lieferobjekt hat ein Zeitbudget, kein Geldbudget.
- Ein Task kann Zeit ausschließlich auf das Budget seines Lieferobjekts buchen. Eine Überschreitung des Budgets ist erlaubt.
- Die Zeiterfassung und das Buchen von Zeiten werden erst später implementiert.
- Die Administration ist nur für Administratoren zugänglich.
- Die Datenbankverbindung bleibt vorerst nicht editierbar.
- Die bisherigen Tasks sind Dummy-Tasks. Sie dürfen für die neue Logik gelöscht werden und müssen nicht migriert werden; noch wurde nichts gelöscht.

### Projektboards und Masterboard

- Jedes Projekt kann ein eigenes Kanbanboard mit individuell angelegten Spalten haben.
- Jede Projektspalte wird einer Kategorie bzw. Spalte des Masterboards zugeordnet. Mehrere Projektspalten können derselben Master-Kategorie zugeordnet sein; ein Projektboard kann deutlich mehr Spalten als das Masterboard haben.
- Ein Task ist auf beiden Boards derselbe Task, keine Kopie. Sein Projekt ergibt sich aus seinem Lieferobjekt.
- Verschieben innerhalb einer Master-Kategorie auf dem Projektboard ändert die Kategorie im Masterboard nicht. Beim Wechsel in eine Projektspalte einer anderen Master-Kategorie wechselt der Task auch im Masterboard in diese Kategorie.
- Wird ein Task im Masterboard über eine Kategoriegrenze verschoben, landet er im Projektboard in der ersten, linken Spalte der Zielkategorie – sowohl beim Verschieben nach rechts als auch nach links.

### Weiteres Vorgehen

- Zuerst Datenmodell und Verwaltung für Kunden, Projekte, Lieferobjekte und deren Zeitbudgets planen; darauf aufbauend die Task-Zuordnung und Projektboards.
- Vor der Umsetzung noch nicht festgelegte Felder, Aktionen und Löschregeln klären. Insbesondere das Verhalten bei fehlender Projektspalte für eine Master-Kategorie und die Behandlung des Backlogs sind noch nicht festgelegt.
- Die vorhandene Benutzerverwaltung mit ihrem rechten Bearbeitungspanel dient als Orientierung für die Bedienung.

Für heute ist Schluss; die Umsetzung beginnt beim nächsten Mal.

## Technische Hinweise

- [Administration](Backend/PrKanban.Backend/wwwroot/admin.html) und [Administrationslogik](Backend/PrKanban.Backend/wwwroot/admin.js) erweitern.
- `Task.Customer` im [Task-Modell](Backend/PrKanban.Backend/Models/Task.cs) ist bisher Freitext. Die Dummy-Tasks müssen nicht in das neue Modell übernommen werden.
- Datenmodell, Admin-APIs, verpflichtende Task-Lieferobjekt-Zuordnung und Projektboards einschließlich Berechtigungen und Übersetzungen für DE/EN ergänzen.
- Die Zuordnung von Projektspalten zum Masterboard und die Positionen beim Verschieben konsistent speichern. Die jeweilige Kartenreihenfolge pro Board ist noch als Entwurfsentscheidung zu klären.
- SQLite-Migrationen für bestehende und neue Datenbanken prüfen: Der Start verwendet derzeit sowohl `EnsureCreated()` als auch `Migrate()`.
- Tests mit einer isolierten Datenbank durchführen; keine echten Benutzer- oder sonstigen Daten für Tests verändern.
- Datenbankdateien und konkrete Nutzdaten nicht committen. Die bereits geänderte persönliche `.vscode/settings.json` nicht ungefragt verändern oder committen.
