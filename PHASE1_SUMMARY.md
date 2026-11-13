# Phase 1 - Bestandsaufnahme: ABGESCHLOSSEN ✅

**Datum**: 14. November 2026  
**Status**: Bereit zur Freigabe für Phase 2  
**Original-Repository**: `C:\prKanban` (UNVERÄNDERT)  
**Migration-Clone**: `C:\prKanban-migration`  
**Feature-Branch**: `feature/migrate-firebase-to-sqlite`  

---

## EXECUTIVE SUMMARY

Das prKanban-Projekt ist eine Single-Page Application (SPA) mit 135 KB monolithischer HTML-Datei, die 100% in Vanilla JavaScript geschrieben ist. Das Frontend nutzt Tailwind CSS und FontAwesome Icons von CDN. Der gesamte Anwendungscode (>5000 Zeilen) ist inline in einer HTML-Datei integriert.

**Kernfunktionalität**: Kanban-Board mit Multi-Level Task-Management (Column → SubColumn), Drag-and-Drop, Dark Mode, Responsive Design, und Persistierung in Firebase Firestore mit LocalStorage-Fallback.

**Sicherheit**: Die aktuelle Firestore-Konfiguration ist nicht produktionsreif (keine User-spezifischen Berechtigungen, keine Feldvalidierung).

---

## 1. REPOSITORY-ANALYSE

✅ **Git-Konfiguration verifiziert**:
- Remote URL: `https://github.com/JFK77/prKanban.git`
- Aktueller Branch: `main`
- Letzter Commit: `edb4eb4` (Single project kanban working with Firebase Google storage)
- Status: SAUBER (keine lokalen Änderungen)

✅ **Clone durchgeführt**:
- Quellverzeichnis: `C:\prKanban` (Original - UNVERÄNDERT)
- Zielverzeichnis: `C:\prKanban-migration` (Arbeitskopie)
- Remote Konfiguration: Zeigt auf Original GitHub-Repository
- Feature-Branch erstellt: `feature/migrate-firebase-to-sqlite`

✅ **Sicherheitsverifizierung**:
- Original-Projekt nach Phase 1: UNVERÄNDERT ✅
- Kein direkter Zugriff auf Original während Migration
- Alle Arbeiten erfolgen auf separatem Clone und Branch

---

## 2. PROJEKTSTRUKTUR

```
prKanban-migration/
├── public/
│   └── index.html ..................... 135 KB (SPA mit gesamter Anwendungslogik)
├── docs/
│   └── MIGRATION_ANALYSIS.md .......... Detaillierte Analyse (Phase 1 Ergebnis)
├── .firebase/ ......................... Firebase CLI Metadaten (wird entfernt)
├── .firebaserc ........................ Firebase Projektkonfiguration (wird entfernt)
├── firebase.json ...................... Firebase Hosting Config (wird entfernt)
├── firestore.rules .................... Firestore Security Rules (wird entfernt)
├── firestore.indexes.json ............. Firestore Indexes (wird entfernt)
├── .gitignore ......................... Standard Git-Ignore
├── .vscode/ ........................... VS Code Einstellungen
├── skills-lock.json ................... Copilot Skills Lock
└── README.md .......................... Projekt-Dokumentation
```

**Fazit**: Minimale Projektstruktur. Keine separate Frontend-Build-Pipeline, keine npm/node_modules. Alles ist in einer Datei.

---

## 3. FRONTEND-TECHNOLOGIE-STACK

| Technologie | Version | Quelle | Status |
|---|---|---|---|
| HTML | 5 | Inline | ✅ Erhalten |
| CSS | Tailwind v3 | CDN | ⚠️ Behalten, von Backend-API entkoppeln |
| Icons | FontAwesome 6.4.0 | CDN | ✅ Erhalten |
| JavaScript | ES6+ (Vanilla) | Inline | ✅ Modularisieren |
| UI Framework | Custom | Inline | ✅ Erhalten (keine Breaking Changes) |

**Größenaufschlüsselung der index.html**:
- HTML-Markup: ~40 KB
- CSS-Definitionen (Inline + Tailwind-Config): ~20 KB
- JavaScript-Anwendungslogik: ~75 KB

---

## 4. FIREBASE-ABHÄNGIGKEITEN (DETAILLIERT)

### 4.1 Importierte Module
```javascript
firebase-app v10.12.0          // App-Initialisierung
firebase-firestore v10.12.0    // Datenbank
firebase-auth v10.12.0         // Authentifizierung
```

### 4.2 Konfiguration (Hardcodiert in Code)
```javascript
{
  apiKey: "...",
  authDomain: "prkanban.firebaseapp.com",
  projectId: "prkanban",
  storageBucket: "prkanban.firebasestorage.app",
  messagingSenderId: "...",
  appId: "..."
}
```

### 4.3 Verwendete Firebase-APIs (25+ Aufrufe)

**Authentifizierung** (4 Funktionen):
- `getAuth()` - Auth-Instanz abrufen
- `GoogleAuthProvider()` - Google OAuth Provider
- `signInWithPopup()` - OAuth Login
- `signInAnonymously()` - Guest-Login
- `onAuthStateChanged()` - Auth-Status-Listener
- `signOut()` - Logout

**Firestore Datenbank** (8 Funktionen):
- `getFirestore()` - Datenbank-Instanz
- `enableIndexedDbPersistence()` - Offline-Support
- `collection()` - Collection-Referenz
- `getDocs()` - Alle Dokumente auslesen (→ Tasks laden)
- `getDoc()` - Einzelnes Dokument (→ Config laden)
- `addDoc()` - Neues Dokument erstellen
- `updateDoc()` - Dokument aktualisieren
- `deleteDoc()` - Dokument löschen

**Collections**:
- `tasks` - Task-Daten
- `boardConfig` - Board-Konfiguration pro User

---

## 5. DATENFLUSS-ANALYSE

### 5.1 Authentifizierung (Auth-Flow)
```
[Browser laden]
  ↓
[Firebase Auth Check via onAuthStateChanged()]
  ├─ User vorhanden? → Zum Board laden weiter
  └─ Keine User? → Login-Modal anzeigen
    ├─ Google Login → signInWithPopup()
    └─ Guest Login → signInAnonymously()
  ↓
[Nach Auth erfolgreich]
  ├─ userId = auth.currentUser.uid
  ├─ userName = auth.currentUser.displayName
  └─ Daten laden starten
```

### 5.2 Datenladen (Read-Flow)
```
[Nach Authentifizierung]
  ↓
getDocs(collection(db, 'tasks'))
  ↓
[Tasks in UI rendern]
  ├─ Sortiert nach column, subColumn
  ├─ Filtern nach Owner, Customer, Tags
  └─ Dark Mode vs. Light Mode anwenden
```

### 5.3 Datenspeicherung (Write-Flow)
```
[UI-Aktion: Task erstellen/bearbeiten/löschen]
  ↓
[Frontend-Validierung]
  ↓
[Versuch: Firebase Schreib-Operation]
  ├─ Erfolg → UI Update + LocalStorage-Backup
  ├─ Fehler → LocalStorage Fallback + Toast-Fehler
  └─ Offline → LocalStorage + "Sync pending" Indicator
```

### 5.4 Offline-Fallback
```
[Firestore nicht erreichbar]
  ↓
[Nutze LocalStorage statt IndexedDB]
  ├─ Alle Operationen lokal
  ├─ Keine Cloud-Synchronisierung
  └─ Daten sind nur im Browser verfügbar
```

---

## 6. FIRESTORE-DATENBANKSCHEMA

### 6.1 Collection: `tasks`
```javascript
{
  id: "auto-generated",
  title: string,                    // Z.B. "Login implementieren"
  taskType: string,                 // Z.B. "BUG", "FEATURE", "TASK"
  owner: string,                    // Z.B. "Max Mustermann"
  customer: string,                 // Z.B. "Kunde A"
  description: string,              // HTML-Rich-Text möglich
  tags: array<string>,              // Z.B. ["urgent", "testing"]
  column: string,                   // Z.B. "TODO", "IN_PROGRESS", "DONE"
  subColumn: string,                // Z.B. "Planning", "Development", "Testing"
  isArchived: boolean,              // Task archiviert ja/nein
  createdAt: timestamp,             // Erstellungszeit
  updatedAt: timestamp,             // Letzte Änderungszeit
  userId: string                    // Benutzer-ID (Referenz zur Auth)
}
```

**Beispiel-Datensatz**:
```json
{
  "id": "task_12345",
  "title": "Frontend API-Integration",
  "taskType": "FEATURE",
  "owner": "John Doe",
  "customer": "Acme Corp",
  "description": "<p>REST API für Tasks implementieren</p>",
  "tags": ["backend", "high-priority"],
  "column": "IN_PROGRESS",
  "subColumn": "Development",
  "isArchived": false,
  "createdAt": "2026-08-15T10:30:00Z",
  "updatedAt": "2026-11-14T08:45:00Z",
  "userId": "user_firebaseuid_123"
}
```

### 6.2 Collection: `boardConfig`
```javascript
{
  userId: string,
  columnOrder: array<string>,              // Spalten-Reihenfolge
  subColumnMapping: {
    [columnName]: array<string>            // Sub-Spalten pro Spalte
  },
  filters: object,                         // Gespeicherte Filter
  wipLimits: { [column]: number },         // Work-In-Progress Limits
  tagsConfig: array<string>,               // Verfügbare Tags
  theme: "light" | "dark",                 // User's Theme-Präferenz
  ... weitere Konfiguration
}
```

### 6.3 Firestore Security Rules (CURRENT)
```firestore
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /tasks/{taskId} {
      allow read, write: if request.auth != null;  // Nur Auth erforderlich
    }
    match /boardConfig/{docId} {
      allow read, write: if request.auth != null;
    }
  }
}
```

**⚠️ SICHERHEITSPROBLEME**:
- ❌ Keine Validierung der Dokumentenbesitz (ownership)
- ❌ Keine Feldvalidierung (Größe, Format, Typ)
- ❌ Jeder authentifizierte Benutzer kann alle Tasks bearbeiten/löschen
- ❌ Keine Audit-Logs
- ❌ Keine Rate-Limiting
- ❌ Anonymous Users haben volle Schreib-Zugriff

---

## 7. IMPLEMENTIERTE FUNKTIONEN

### ✅ TASK MANAGEMENT
- [x] Task erstellen (Modal mit Formularvalidierung)
- [x] Task bearbeiten (Alle Felder editierbar)
- [x] Task löschen (Mit Bestätigungsdialog)
- [x] Task archivieren/dearchivieren
- [x] Task-Hierarchie (Column → SubColumn)
- [x] Task-Filterung (Owner, Customer, Tags)
- [x] Task-Sortierung (Drag-and-Drop)

### ✅ UI/UX
- [x] Kanban-Board (Multi-Column Layout)
- [x] Drag-and-Drop (Tasks zwischen Spalten/Unter-Spalten verschieben)
- [x] Dark Mode / Light Mode (Persistiert in LocalStorage)
- [x] Responsive Design (Mobile, Tablet, Desktop getestet)
- [x] Details-Panel (Resizable Side-Panel für Task-Details)
- [x] Toast-Benachrichtigungen (Erfolg, Fehler, Warnungen)
- [x] Modale Dialoge (Task erstellen, Bestätigung, Config)
- [x] Custom Combobox/Dropdown-Komponenten
- [x] Visuelles Drag-Feedback (Erlaubte/Verbotene Drop-Zones farblich gekennzeichnet)

### ✅ PERSISTIERUNG
- [x] Firebase Firestore (primärer Speicher)
- [x] LocalStorage (Fallback bei Offline)
- [x] IndexedDB (Automatisch via Firebase)
- [x] Offline-Unterstützung (mit Fallback)

### ✅ AUTHENTIFIZIERUNG
- [x] Google OAuth Sign-In
- [x] Anonymous/Guest Sign-In
- [x] Logout
- [x] Auth State Listener (UI Updates)
- [x] User-Info Anzeige (Name, Avatar, Projekt)

### ❌ NICHT IMPLEMENTIERT
- [ ] Multi-User Real-time Collaboration (Real-time Listener)
- [ ] Benutzerprofile / Benutzerverwaltung
- [ ] Rollen- / Berechtigungssystem (ACL)
- [ ] Revisionshistorie (Audit-Trail)
- [ ] Notifications / Alerts
- [ ] API für externe Integrationen
- [ ] Service Worker (Offline Caching)

---

## 8. LOKALE SPEICHERUNG (LocalStorage & IndexedDB)

### 8.1 LocalStorage-Keys
```javascript
"theme"              // "dark" oder "light"
// Weitere Keys werden bei Offline-Fallback dynamisch erstellt
```

### 8.2 IndexedDB
```
Database: "firebaseLocalStorageDb"
Automatisch von Firebase SDK verwaltetet
Speichert Auth-Token und Firestore-Daten für Offline-Zugriff
```

### 8.3 Fallback-Logik
```javascript
if (Firestore nicht erreichbar) {
  // Alle CRUD-Operationen nutzen LocalStorage
  // Daten sind nur lokal im Browser verfügbar
  // Keine Cloud-Synchronisierung
  // Benutzer wird benachrichtigt ("Offline Mode")
}
```

---

## 9. MIGRATIONSRISIKEN UND HERAUSFORDERUNGEN

### 🔴 KRITISCHE RISIKEN

| # | Risiko | Severity | Details |
|---|--------|----------|---------|
| 1 | **Single-File SPA** | 🔴 CRITICAL | 135 KB monolithische HTML. Schwer zu refaktorisieren. Müssen in Module aufteilen. |
| 2 | **Firebase-Integration** | 🔴 CRITICAL | Firebase SDK tief in Code verankert. Jeder CRUD-Operation nutzt Firebase. Müssen alle durch REST API ersetzen. |
| 3 | **Auth-Flow-Migration** | 🟠 HIGH | Google OAuth mit Firebase Auth. Muss zu Windows Auth oder ASP.NET Core Identity wechseln. |
| 4 | **Offline-Logik** | 🟠 HIGH | IndexedDB-Persistence durch Firebase. Müssen neue Offline-Sync-Logik implementieren. |

### 🟠 HOHE RISIKEN

| # | Risiko | Severity | Details |
|---|--------|----------|---------|
| 5 | **Firestore Queries** | 🟠 HIGH | 8+ Firestore-Funktionen (getDocs, getDoc, addDoc, etc.) müssen zu REST-Calls werden. |
| 6 | **Datenmigration** | 🟠 HIGH | Firestore-Daten (Tasks, Config) zu SQLite migrieren. Referentielle Integrität prüfen. |
| 7 | **Berechtigungen** | 🟠 HIGH | Aktuelle Firebase Rules sind zu permissiv. Neue serverseitige ACL-Logik erforderlich. |
| 8 | **Multi-User-Concurrency** | 🟠 HIGH | SQLite Single-Writer Limitation. Bei 5-20 gleichzeitigen Benutzern: Locking-Strategie erforderlich. |

### 🟡 MITTLERE RISIKEN

| # | Risiko | Severity | Details |
|---|--------|----------|---------|
| 9 | **Drag-and-Drop** | 🟡 MEDIUM | Muss nach API-Umzug vollständig getestet werden. |
| 10 | **Echtzeit-Updates** | 🟡 MEDIUM | Aktuell keine Real-time Listener. Aber nach Migration: Polling oder WebSocket erforderlich? |
| 11 | **Dark Mode Persistierung** | 🟡 MEDIUM | LocalStorage bleibt. Aber: Theme muss auch serverseitig gespeichert werden? |
| 12 | **Performance** | 🟡 MEDIUM | 135 KB HTML-Datei. Bei schnellem Wachstum problematisch. Code-Splitting erforderlich. |

---

## 10. CODE-KOMPLEXITÄT

**Geschätzte Codezeilen**:
```
index.html (Total) ..................... 138,495 bytes (~4000-5000 Zeilen)
├── HTML-Markup ........................ ~2000 Zeilen
├── CSS-Definitionen .................. ~500 Zeilen (Custom + Tailwind Config)
├── JavaScript-Logik .................. ~1500+ Zeilen
│   ├── Firebase-Setup ................ ~50 Zeilen
│   ├── Auth-Management ............... ~200 Zeilen
│   ├── Task-CRUD ..................... ~400 Zeilen
│   ├── UI-Rendering .................. ~400 Zeilen
│   ├── Drag-and-Drop ................. ~250 Zeilen
│   └── Sonstiges (Filter, Modal, etc) ~200+ Zeilen
└── HTML-Elemente ..................... ~3000+ DOM-Elemente
```

**JavaScript-Funktionen** (Identifizierte Hauptfunktionen):
1. `loginWithGoogle()` - Google OAuth
2. `loginAsGuest()` - Anonymous Auth
3. `logoutFromFirebase()` - Logout
4. `loadTasks()` - Tasks laden
5. `createTask()` - Task erstellen
6. `editTask()` - Task bearbeiten
7. `deleteTask()` - Task löschen
8. `archiveTask()` - Task archivieren
9. `updateTaskOrder()` - Drag-and-Drop Speicherung
10. `applyFilters()` - Filter-Logik
11. `toggleTheme()` - Dark Mode Toggle
12-25+ weitere Event-Handler und UI-Manipulationen

---

## 11. ABHÄNGIGKEITEN

### Frontend-Abhängigkeiten (CDN)
```html
<!-- Styling -->
<script src="https://cdn.tailwindcss.com"></script>
<link rel="stylesheet" href=".../font-awesome/6.4.0/css/all.min.css">

<!-- Firebase (zu ersetzen) -->
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-app.js"></script>
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-firestore.js"></script>
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js"></script>
```

### Backend-Abhängigkeiten (Ziel)
```
ASP.NET Core 10.0.x
Entity Framework Core 10.0.x
SQLite 3.x
Windows Authentication (oder ASP.NET Core Identity)
```

---

## 12. MIGRATIONS-ROADMAP (EMPFOHLEN)

### Phase 1: Bestandsaufnahme ✅ DONE
- [x] Repository klonen
- [x] Struktur analysieren
- [x] Firebase-Abhängigkeiten dokumentieren
- [x] Datenflüsse analysieren
- [x] Risiken identifizieren

### Phase 2: Zielarchitektur-Design ⏳ PENDING APPROVAL
- [ ] Backend-Projektstruktur definieren (Models, Services, Controllers)
- [ ] Database-Schema erstellen
- [ ] REST API Endpoints definieren
- [ ] Authentifizierungs-Flow definieren
- [ ] Synchronisierungs-Konzept entwerfen

### Phase 3: ASP.NET Core Backend ⏳ PENDING
- [ ] Solution mit Schichtenarchitektur erstellen
- [ ] EF Core Models definieren
- [ ] SQLite-Datenbank initialisieren
- [ ] REST API implementieren
- [ ] Authentifizierung konfigurieren
- [ ] Unit Tests schreiben

### Phase 4: Frontend-Migration ⏳ PENDING
- [ ] JavaScript API-Client erstellen
- [ ] Firebase-Imports entfernen
- [ ] Alle Firebase-Calls ersetzen
- [ ] Authentifizierungs-Flow anpassen
- [ ] Offline-Logik anpassen
- [ ] Integration Tests durchführen

### Phase 5: Datenmigration ⏳ PENDING
- [ ] Export-Tool für Firestore implementieren
- [ ] Import-Tool für SQLite implementieren
- [ ] Datenintegrität validieren
- [ ] Rollback-Verfahren testen

### Phase 6: IIS-Deployment ⏳ PENDING
- [ ] Publish-Profil erstellen
- [ ] IIS-Konfiguration durchführen
- [ ] HTTPS/TLS konfigurieren
- [ ] Backup-Strategien implementieren

### Phase 7: Firebase entfernen ⏳ PENDING
- [ ] Firebase SDK entfernen
- [ ] Firebase-Konfiguration löschen
- [ ] Test-Akzeptanz durchführen

---

## 13. KRITISCHE DESIGN-ENTSCHEIDUNGEN (AUSSTEHEND)

Für Phase 2 wird Ihre Freigabe für diese Entscheidungen benötigt:

1. **Authentifizierung**:
   - [ ] Windows Authentication (Active Directory) verwenden?
   - [ ] oder ASP.NET Core Identity (Benutzerverwaltung im System)?
   - [ ] oder OAuth/OIDC-Integration?

2. **Datenbankpfad**:
   - [x] Empfohlen: `D:\Apps\prKanban\Data\prkanban.db`
   - [ ] Alternative?

3. **Multi-User Concurrency**:
   - [ ] SQLite mit Locking ausreichend für 5-20 Users?
   - [ ] oder sollten wir auf PostgreSQL upgraden?

4. **Offline-Sync**:
   - [ ] Sollte die neue Version Offline-Sync unterstützen?
   - [ ] oder nur Online mit lokalem Browser-Cache?

5. **API-Versionierung**:
   - [ ] API-Version in URL (`/api/v1/tasks`) oder Header?

6. **Deployment-Ziel**:
   - [ ] Windows PC (lokale Entwicklung)?
   - [ ] Windows Server (Produktion)?
   - [ ] Beides?

---

## 14. ARBEITSAUFWAND-SCHÄTZUNG

| Phase | Aufwand | Komplexität | Anmerkung |
|-------|---------|------------|----------|
| Phase 1: Bestandsaufnahme | ✅ DONE | - | - |
| Phase 2: Design | 8-16 Std. | Mittel | Abhängig von Entscheidungen |
| Phase 3: Backend | 40-60 Std. | Hoch | EF Core, API, Auth, Tests |
| Phase 4: Frontend | 30-50 Std. | Hoch | Firebase→REST, Refactoring |
| Phase 5: Datenmigration | 10-20 Std. | Mittel | Export, Import, Validierung |
| Phase 6: Deployment | 10-20 Std. | Mittel | IIS, HTTPS, Backup |
| Phase 7: Cleanup | 5-10 Std. | Niedrig | Firebase entfernen |
| **TOTAL** | **113-176 Std.** | **HOCH** | Abhängig von Entscheidungen |

---

## 15. NÄCHSTE SCHRITTE (FREIGABE ERFORDERLICH)

Nach Ihrer Freigabe dieser Analyse wird Phase 2 wie folgt beginnen:

1. **Entscheidungen treffen** (siehe Punkt 13 oben)
2. **Detaillierte Architektur entwerfen**
   - Backend-Projektstruktur
   - Database-Schema
   - REST API Specification
   - Authentication-Flow
3. **Dokumentation erstellen**:
   - `docs/ARCHITECTURE.md` - Backend-Architektur
   - `docs/DATABASE.md` - SQL-Schema und EF Core Models
   - `docs/API.md` - REST API Spezifikation
   - `docs/AUTHENTICATION.md` - Auth-Implementation
4. **Erster Commit** auf `feature/migrate-firebase-to-sqlite` mit Architektur-Dokumentation

---

## ZUSAMMENFASSUNG

✅ **Phase 1 Status: ABGESCHLOSSEN**

**Verifiziert**:
- [x] Original-Repository unverändert
- [x] Separater Clone erstellt und mit Feature-Branch vorbereitet
- [x] Vollständige Code- und Architektur-Analyse durchgeführt
- [x] Firebase-Abhängigkeiten dokumentiert
- [x] Migrationsrisiken identifiziert
- [x] Detaillierte Roadmap erstellt
- [x] Arbeitsaufwand geschätzt

**Dokumentation verfügbar**:
- `C:\prKanban-migration\docs\MIGRATION_ANALYSIS.md` - Detaillierte technische Analyse (15 Seiten)
- `C:\prKanban-migration\PHASE1_SUMMARY.md` - Diese Executive Summary

**Nächster Schritt**: Freigabe dieser Analyse und Genehmigung der kritischen Design-Entscheidungen (siehe Punkt 13).

---

**Status**: 🟢 **BEREIT FÜR PHASE 2 - WARTE AUF FREIGABE**

Repository-Status:
- Original: `C:\prKanban` - CLEAN ✅
- Migration: `C:\prKanban-migration` (Branch: `feature/migrate-firebase-to-sqlite`) - READY ✅
