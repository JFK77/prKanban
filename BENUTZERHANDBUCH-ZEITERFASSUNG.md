# Zeiterfassung und Schätzung - Benutzerhandbuch

## 🎯 Feature-Übersicht

Das Zeiterfassungs-Feature ermöglicht es, die aufgewendete Zeit für Tasks zu protokollieren und zu verwalten. Die Funktionen sind schlank gehalten und fokussieren sich auf die Essentials:

- **Zeiterfassung**: Erfassen Sie geleistete Stunden gegen Tasks
- **Schätzung**: Definieren Sie geschätzte Stunden für Planung
- **Burndown**: Verfolgen Sie Fortschritt gegen Schätzung in Echtzeit
- **Billable Tracking**: Unterscheiden Sie abrechenbare vs. nicht-abrechenbare Zeit

---

## 📍 Wo können Sie Zeit eintragen?

### In der Task öffnen:
1. Klicken Sie auf eine Task im Kanban-Board, um das Task-Panel zu öffnen
2. Oben sehen Sie drei Tabs: **Details** | **Zeiterfassung** | **Schätzung**
3. Klicken Sie auf den Tab **"Zeiterfassung"**

### Im Zeiterfassungs-Tab:
Das Formular hat folgende Felder:

| Feld | Beschreibung | Erforderlich |
|------|-------------|------------|
| **Anfang** | Datum und Zeit, wann die Arbeit startete | Ja |
| **Ende** | Datum und Zeit, wann die Arbeit endete | Optional* |
| **Dauer** | Dauer in Stunden (z.B. 1.5) | Optional* |
| **Kommentar** | Optionale Notiz (z.B. "Anforderungen analysiert") | Nein |
| **Abrechenbar** | Toggle: Ist diese Zeit dem Kunden in Rechnung zu stellen? | Nein (Standard: Ja) |

*Entweder "Ende" oder "Dauer" muss ausgefüllt sein

### Zeiteinträge hinzufügen:
1. Füllen Sie das Formular aus
2. Klicken Sie den Button **"Hinzufügen"** (grüner Button)
3. Der Eintrag wird gespeichert und in der Liste unten angezeigt
4. Die Summen (Gesamtstunden, abrechenbare Stunden) werden automatisch aktualisiert

---

## 📊 Wo können Sie die Schätzung eintragen?

### Im Schätzungs-Tab:
1. Öffnen Sie eine Task
2. Klicken Sie auf den Tab **"Schätzung"**
3. Geben Sie die **"Geschätzter Aufwand (Stunden)"** ein
4. Das System berechnet automatisch:
   - **Geschätzt**: Die von Ihnen eingegebenen Stunden
   - **Erfasst**: Summe aller Zeiteinträge für diese Task
   - **Verbleibend**: Geschätzt minus Erfasst
   - **Auslastung**: Erfasst / Geschätzt als Prozentanzahl mit visueller Fortschrittsleiste

### Burndown-Visualisierung:
- Der Fortschrittsbalken zeigt visuell, wie viel Zeit bereits aufgewendet wurde
- Rot/Orange bedeutet: Sie überschreiten die Schätzung
- Grün bedeutet: Sie sind im Budget

---

## 💰 Wo können Sie Budget eintragen?

Das Zeit-Budget wird auf **Lieferobjekt-Ebene** (Deliverable) eingetragen:

### Budget im Lieferobjekt definieren:
1. Navigieren Sie zur **Hierarchie-Ansicht** (Kunde > Projekt > Lieferobjekt)
2. Öffnen Sie ein **Lieferobjekt**
3. Tragen Sie das **Zeitbudget** ein
4. Speichern Sie die Änderung

### Budget-Summen im Projekt:
- Das Budget wird automatisch zum **Projekt-Level** summiert
- Alle Lieferobjekt-Budgets unter einem Projekt werden addiert
- Budget-Summen werden im Project mit hellgrau und kursiv angezeigt

### Budget vs. Task-Schätzung:
- **Budget**: Gesamtstundenbudget für das Lieferobjekt/Projekt
- **Task-Schätzung**: Wie viel Zeit für diese spezifische Task geplant ist

---

## 📈 Übersicht der Metriken

### Auf Task-Ebene (Zeiterfassungs-Tab):
```
Eingetragene Zeiten:
├── Summe: 8.5 Stunden (alle Einträge addiert)
├── Abrechenbar: 7.5 Stunden (nur markierte Einträge)
└── Nicht abrechenbar: 1.0 Stunde (Einträge ohne Haken)
```

### Auf Task-Ebene (Schätzungs-Tab):
```
Burndown-Status:
├── Geschätzt: 10.0 h
├── Erfasst: 8.5 h
├── Verbleibend: 1.5 h
└── Auslastung: 85% ████████░
```

### Auf Projekt-Ebene:
```
Budget-Summen:
├── Gesamtbudget: 160 h (Summe aller Lieferobjekte)
├── _Davon summiert_: _Hellgrau und kursiv angezeigt_
└── Überbuchen: Ist möglich (kein Hard-Limit)
```

---

## ✨ Besonderheiten

### Keine monetären Werte
- Das System erfasst **nur Zeit**, nicht Kosten
- Billable vs. Non-Billable ist für spätere Abrechnung relevant
- Keine automatische Kostenberechnung

### Flexible Zeit-Eingabe
- Geben Sie **Start und End-Zeit** ein, ODER
- Geben Sie **nur die Dauer** in Stunden ein
- Das System berechnet was fehlt

### Keine Freigabe erforderlich
- Zeiteinträge können jederzeit bearbeitet/gelöscht werden
- Es gibt keinen Freigabe- oder Genehmigungsprozess
- Der Ersteller kann seine Einträge selbst verwalten

### Automatische Berechnung
- Verbleibende Stunden = Schätzung - Erfasst
- Auslastung % = (Erfasst / Schätzung) × 100
- Alles wird in Echtzeit berechnet

---

## 🔧 Workflow-Beispiel

### Szenario: Task "Datenbank-Design" (10 Stunden geschätzt)

**Schritt 1: Schätzung eintragen**
- Task öffnen → Schätzungs-Tab → "10" Stunden eingeben

**Schritt 2: Erste Zeit erfassen** (Montag, 3 Stunden)
- Zeiterfassungs-Tab
- Anfang: 2026-11-21 09:00
- Dauer: 3
- Kommentar: "Datenbankmodell analysiert"
- Abrechenbar: ✓ (angehakt)
- Klick: Hinzufügen

**Schritt 3: Weitere Zeit erfassen** (Dienstag, 4 Stunden)
- Anfang: 2026-11-22 10:00
- Ende: 2026-11-22 14:00
- Kommentar: "Tabellen und Beziehungen erstellt"
- Abrechenbar: ✓
- Klick: Hinzufügen

**Schritt 4: Burndown anschauen** (Schätzungs-Tab)
- Geschätzt: 10 h
- Erfasst: 7 h
- Verbleibend: 3 h
- Auslastung: 70% ███████░

---

## ❓ Häufig gestellte Fragen

**F: Kann ich Zeit rückwirkend eintragen?**
A: Ja! Sie können jedes beliebige Datum/Zeit eintragen.

**F: Kann ich Zeit bearbeiten?**
A: Der aktuelle Stand ermöglicht Löschen und Neu-Erfassen. Direktes Bearbeiten ist geplant.

**F: Was bedeutet "Abrechenbar"?**
A: Markiert Zeit, die dem Kunden in Rechnung gestellt wird. Dient zur Unterscheidung von administrativer Zeit.

**F: Kann das Budget überschritten werden?**
A: Ja! Überbuchen ist möglich. Es gibt kein hartes Limit, nur eine Warnung durch die Auslastung > 100%.

**F: Wo werden die Zeit-Daten angezeigt?**
A: 
- In der Task (Zeiterfassungs-Tab)
- In der Tabellensicht (Spalte "Zeit")
- Im Burndown (Schätzungs-Tab)

**F: Kann ich Zeit-Einträge löschen?**
A: Ja! Klicken Sie auf das Papierkorb-Icon neben dem Eintrag.

---

## 📱 Responsive Design

Das Feature funktioniert auf:
- ✓ Desktop-Browsern
- ✓ Tablets
- ✓ Mobilen Geräten

Alle Eingabefelder sind touch-freundlich und responsive gestaltet.

---

## 🌙 Dark Mode

Das Feature unterstützt vollständig:
- ✓ Light Mode
- ✓ Dark Mode
- Farbschema passt sich automatisch an

---

## 📝 Weitere Hinweise

### Datenschutz
- Alle Zeiteinträge werden mit dem eingeloggten Benutzer verknüpft
- Der Ersteller wird automatisch erfasst
- Nur autorisierte Benutzer können Zeiteinträge sehen/ändern

### Performance
- Zeiteinträge werden nur geladen, wenn Sie den Zeiterfassungs-Tab öffnen
- Schätzung wird in Echtzeit berechnet
- Keine zusätzliche Datenbankabfrage für Burndown

### Internationalisierung
- Alle Felder unterstützen deutsche Sprache
- Datumsformat: Locale-spezifisch (Deutsch)
- Alle UI-Texte sind für Mehrsprachigkeit vorbereitet

---

**Version**: 1.0
**Letztes Update**: 2026-11-21
**Status**: ✅ Produktionsreif
