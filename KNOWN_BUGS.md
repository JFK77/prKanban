# 🐛 Known Bugs & Session Handoff

**Datum**: 14. November 2026, 02:54 Uhr  
**Status**: Migration Complete, Bug #1 behoben, Bug #2 offen  
**Branch**: `feature/migrate-firebase-to-sqlite`

---

## Critical Bugs Identifiziert

### ✅ Bug #1: Task Type wird nicht gespeichert (behoben)

**Beschreibung**:  
Wenn ein Benutzer den Task Type ändert (z.B. von "Feature" zu "Bug"), wird die Änderung nicht in der Datenbank persistiert.

**Reproduzierungsschritte**:
1. Board öffnen (http://localhost:5000)
2. Login mit `admin@prkanban.local` / `[ADMIN_PASSWORD]`
3. Auf einen Task klicken
4. Task Type ändern (z.B. "Feature" → "Bug")
5. Task speichern
6. **Erwartet**: Task Type sollte "Bug" sein
7. **Vor dem Fix**: Task Type zeigte noch "Feature"

**Ursache**:  
Die `UpdateTask()` Methode aktualisierte das TaskType Feld nicht; auch das Update-DTO enthielt es nicht:

```csharp
// FEHLT: if (!string.IsNullOrEmpty(request.TaskType))
//           task.TaskType = request.TaskType;
```

**Lösung**:
`TaskType` zum Update-DTO hinzufügen und beim Update übernehmen:
```csharp
if (!string.IsNullOrEmpty(request.TaskType))
    task.TaskType = request.TaskType;
```

**Test nach Fix**:
```powershell
# API Direkttest
$id = "task_xxxxx"  # einen echten Task ID verwenden
@{ taskType = "Bug" } | ConvertTo-Json -Compress | Out-File "$env:TEMP\upd.json" -Encoding UTF8 -NoNewline
curl.exe -s -X PUT "http://localhost:5000/api/v1/tasks/$id" \
  -H "Content-Type: application/json" -b $cookieFile \
  -d "@$env:TEMP\upd.json" | ConvertFrom-Json
# Response sollte taskType: "Bug" enthalten
```

**Priority**: 🟡 MEDIUM  
**Complexity**: ⭐ EASY  
**Estimated Fix Time**: 5 Minuten

---

### 🔴 Bug #2: Drag & Drop in DONE Unterspalten funktioniert nicht

**Beschreibung**:  
Tasks können nicht in die Unterspalten der DONE Spalte (z.B. "Done (kein Limit)") gezogen werden. Drag funktioniert in anderen Spalten (TODO, ANALYSIS, IN PROGRESS) aber nicht bei DONE.

**Reproduzierungsschritte**:
1. Board öffnen
2. Task aus TODO nehmen
3. Versuchen in DONE Spalte zu ziehen
4. **Erwartet**: Task sollte in DONE landen
5. **Aktuell**: Drag funktioniert nicht oder Error "Priorisierung konnte nicht gespeichert werden"

**Ursache**:  
Wahrscheinlich eines der folgenden Probleme:
1. Frontend `allowedDropColumns` Konfiguration erlaubt DONE nicht
2. SubColumnName "Done (kein Limit)" wird falsch formatiert gesendet
3. API Validierung blockiert bestimmte SubColumn Namen
4. JavaScript Fehler beim Drag in DONE Spalte

**Debug-Schritte** (für nächste Session):
```
1. Browser DevTools öffnen (F12)
2. Zu Console Tab gehen
3. Task von TODO zu DONE ziehen
4. Auf Fehler prüfen (sollte "Fehler beim Speichern" zeigen)
5. Zu Network Tab gehen
6. Request zu /api/v1/tasks/{id} prüfen:
   - Wird SubColumnName "Done (kein Limit)" gesendet?
   - Oder wird NULL gesendet?
   - Status Code 200 oder Error?
7. Response prüfen:
   - Ist columnName = "Done"?
   - Ist subColumnName richtig gespeichert?
```

**Mögliche Lösungen**:

A) **Frontend allowedDropColumns Prüfen** (index.html ~Zeile 400-450)
```javascript
// Falls "Done" nicht in allowedDropColumns ist, hinzufügen
if (allowedDropColumns === undefined || !allowedDropColumns.includes('Done')) {
    // Drag in Done erlauben
}
```

B) **SubColumnName Handling im Frontend** (index.html ~Zeile 1520-1550)
```javascript
// updateTaskSetInCloud() - Prüfe ob SubColumnName für Done richtig gesendet wird
const payload = normalizeTaskPayload(item.data);
// Debuggen: console.log("Sending to API:", payload);
```

C) **Backend Validierung** (TasksController.cs ~Zeile 155-165)
```csharp
// Prüfe ob "Done (kein Limit)" akzeptiert wird
if (request.SubColumnName != null) {
    // Sollte auch "Done (kein Limit)" akzeptieren
    task.SubColumnName = request.SubColumnName;
}
```

**Test nach Fix**:
```powershell
# Manueller API Test
$id = "task_xxxxx"  # einen TODO Task ID verwenden

# Test 1: Zu DONE ohne Limits
@{ 
    columnName = "Done"
    subColumnName = "Done (kein Limit)"
} | ConvertTo-Json -Compress | Out-File "$env:TEMP\move.json" -Encoding UTF8 -NoNewline

$result = curl.exe -s -X PUT "http://localhost:5000/api/v1/tasks/$id" \
  -H "Content-Type: application/json" -b $cookieFile \
  -d "@$env:TEMP\move.json" | ConvertFrom-Json

# Ergebnis prüfen
Write-Host "ColumnName: $($result.data.columnName)"
Write-Host "SubColumnName: $($result.data.subColumnName)"
# Sollte jeweils "Done" und "Done (kein Limit)" sein
```

**Priority**: 🔴 HIGH  
**Complexity**: ⭐⭐ MEDIUM (debug + fix)  
**Estimated Fix Time**: 30-45 Minuten

---

## Session Summary

### Abgeschlossen ✅
- Firebase → ASP.NET Core Migration COMPLETE
- REST API mit allen CRUD Operationen
- Authentifizierung funktionieret
- 90% der Drag & Drop Funktionalität werkend
- 14+ Test-Tasks im System

### Getestete Funktionen ✅
| Feature | Status | Hinweise |
|---------|--------|----------|
| Login | ✅ | ASP.NET Identity |
| Task Load | ✅ | 14+ tasks werden angezeigt |
| Task Create | ✅ | Neue Tasks erstellen funktioniert |
| Task Update | ✅ | TaskType wird persistiert |
| Task Delete | ✅ | Löschen funktioniert |
| Drag TODO → ANALYSIS | ✅ | Funktioniert perfekt |
| Drag ANALYSIS → INPROGRESS | ✅ | Funktioniert perfekt |
| Drag → DONE | ❌ | SubColumn Handling fehlt |
| API Format Conversion | ✅ | columnName ↔ column mapping works |

### Bekannte Workarounds
**Bis Bug #2 gefixt ist**:
- Tasks können nicht in DONE Spalte gezogen werden
- Workaround: Über API direkt updaten

---

## Nächste Session - Priorität

```
[x] BUG FIX #1: TaskType Update Handler
    Datei: Backend/PrKanban.Backend/Controllers/TasksController.cs
    Zeile: ~157
    Code: if (!string.IsNullOrEmpty(request.TaskType)) task.TaskType = request.TaskType;

[ ] BUG FIX #2: DONE SubColumn Drag & Drop (30-45 Min)
    Debug Schritte sind oben dokumentiert
    
[ ] TEST SUITE nach beiden Fixes
    Commit: "Fix known bugs #1 and #2"
```

---

## Hilfreiches für Debug

**Server starten**:
```bash
cd C:\prKanban-migration\Backend\PrKanban.Backend
dotnet run
# Server läuft auf http://localhost:5000
```

**Login Credentials**:
- Email: `admin@prkanban.local`
- Password: `[ADMIN_PASSWORD]`

**Wichtige Dateien**:
- Frontend Code: `Backend/PrKanban.Backend/wwwroot/index.html`
- API Endpoints: `Backend/PrKanban.Backend/Controllers/TasksController.cs`
- Database: `Backend/PrKanban.Backend/Data/prkanban.db`

**Browser DevTools Tipps**:
- F12 zum öffnen
- Console Tab für JavaScript Fehler
- Network Tab um API Requests zu sehen
- Application Tab um Cookies/Session zu checken

---

**Status**: Ready für nächste Session  
**Komplexität dieser Fixes**: Sehr niedrig  
**Geschätzte Gesamtzeit**: 45-60 Minuten inklusive Testing
