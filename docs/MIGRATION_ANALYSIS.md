# prKanban Migration Analysis
## Firebase → ASP.NET Core 10 + SQLite + IIS

**Datum**: 14. November 2026  
**Status**: Phase 1 - Bestandsaufnahme abgeschlossen  
**Repository**: https://github.com/JFK77/prKanban.git  
**Clone-Pfad**: `C:\prKanban-migration`  
**Feature-Branch**: `feature/migrate-firebase-to-sqlite`  

---

## 1. Projektstruktur

```
prKanban-migration/
├── .agents/                          # Copilot Agents Konfiguration (nicht produktiv)
│   └── skills/                       # Firebase Skills
├── .firebase/                        # Firebase CLI Metadaten (wird entfernt)
├── .firebaserc                       # Firebase Projektkonfiguration (wird entfernt)
├── .vscode/
│   └── settings.json                 # VS Code Einstellungen
├── .git/                             # Git Repository
├── .gitignore
├── public/
│   └── index.html                    # SPA mit 135 KB (Frontend + Anwendungslogik)
├── docs/                             # Migrationsplanungsdokumentation
├── firebase.json                     # Firebase Hosting & Firestore Config (wird entfernt)
├── firestore.indexes.json            # Firestore Indizes (wird entfernt)
├── firestore.rules                   # Firestore Security Rules (wird entfernt)
├── skills-lock.json                  # Copilot Skills Lock
└── README.md                         # Projekt Dokumentation
```

**Dateianzahl**: 
- Anwendungsdateien: 1 (index.html)
- Konfigurationsdateien: 8
- Firebase-spezifische Dateien: 4
- Dokumentation: 1
- Verzeichnisse mit externem Content: .agents/ (Copilot Skills)

---

## 2. Frontend-Analyse

### 2.1 Technologie-Stack

| Komponente | Details |
|-----------|---------|
| **Format** | Single-Page Application (SPA) in einer HTML-Datei |
| **HTML-Version** | HTML5 |
| **CSS** | Tailwind CSS (v3) via CDN |
| **Icons** | FontAwesome 6.4.0 via CDN |
| **JavaScript** | ES6+ (Vanilla JS, keine Framework-Dependencies) |
| **Größe** | 135 KB komplett (HTML + CSS + JS) |
| **Sprache** | Deutsch |

### 2.2 HTML-Struktur

Die `index.html` ist monolithisch aufgebaut:
- **Sektion 1**: `<head>` mit Metadaten, CDN-Imports, Tailwind-Konfiguration, globale Styles
- **Sektion 2**: `<body>` mit kompletter UI-Struktur (Navbar, Boards, Details-Panel, Modals)
- **Sektion 3**: `<script type="module">` mit vollständiger Anwendungslogik (10KB+ JavaScript)

### 2.3 Firebase-Abhängigkeiten (detailliert)

**Importierte Firebase-Module** (via CDN):
```javascript
firebase-app v10.12.0
firebase-firestore v10.12.0
firebase-auth v10.12.0
```

**Firebase-Konfiguration** (hardcodiert):
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

**Verwendete Firebase-Funktionen**:

| Funktion | Zweck |
|----------|-------|
| `initializeApp()` | App-Initialisierung |
| `getFirestore()` | Firestore-Instanz |
| `enableIndexedDbPersistence()` | Offline-Support via IndexedDB |
| `collection(), getDocs(), getDoc()` | Read-Operationen |
| `setDoc(), addDoc(), updateDoc()` | Write-Operationen |
| `deleteDoc()` | Delete-Operationen |
| `writeBatch()` | Transaktionale Operationen |
| `doc()` | Document-Referenzen |
| `getAuth()` | Auth-Instanz |
| `GoogleAuthProvider()` | Google Sign-In |
| `signInWithPopup()` | Google OAuth Flow |
| `signInAnonymously()` | Guest-Login |
| `onAuthStateChanged()` | Auth State Listener |
| `signOut()` | Logout |

**Firebase-Aufrufe im Code**:
- **Initialisierung**: 1x `initializeApp()`, 1x `enableIndexedDbPersistence()`
- **Collections**: `tasks`, `boardConfig`
- **Authentifizierung**: Google OAuth + Anonymous
- **Real-time Listener**: Verwendet nicht; nur One-Time Reads

### 2.4 Lokale Speicherung

**LocalStorage-Keys** (ermittelt):
- `theme` - Dark/Light Mode Präferenz
- Potentiell weitere (Code durchsuchen erforderlich)

**IndexedDB**:
- Firebase Persistence automatisch via `enableIndexedDbPersistence()`

**Fallback-Logik**:
```javascript
if (Firestore nicht erreichbar) {
  → LocalStorage-Fallback aktivieren
}
```

---

## 3. Datenflussanalyse

### 3.1 Laden der Daten (Read-Flow)

```
Browser
  ↓
[Firebase Auth Check: Benutzer authentifiziert?]
  ├─ Ja → Continue
  └─ Nein → Login-Modal
  ↓
[getAuth().onAuthStateChanged()]
  ├─ User vorhanden: Firebase-Daten laden
  │   ├─ getDocs(collection(db, 'tasks')) → Tasks
  │   ├─ getDoc(doc(db, 'boardConfig/{userId}')) → Board-Konfiguration
  │   └─ UI aktualisieren
  └─ User nicht vorhanden: Leere Oberfläche
```

### 3.2 Speicherung von Daten (Write-Flow)

```
UI-Aktion (Task erstellen/bearbeiten/archivieren)
  ↓
[Validierung im Frontend]
  ↓
[Versuch: Firebase Firestore Schreib-Operation]
  ├─ Erfolg → UI Update + LocalStorage Backup
  └─ Fehler → LocalStorage als Fallback + Toast-Nachricht
```

### 3.3 Authentifizierung (Auth-Flow)

```
[Login-Modal angezeigt]
  ├─ Benutzer klickt "Google Login"
  │   ├─ GoogleAuthProvider initialisieren
  │   ├─ signInWithPopup()
  │   └─ OAuth Callback
  │
  └─ Benutzer klickt "Guest Login"
      ├─ signInAnonymously()
      └─ Anonymous UID als Benutzer-ID

[Nach erfolgreicher Auth]
  ├─ userId aus auth.currentUser.uid
  ├─ userName aus auth.currentUser.displayName
  ├─ Avatar aus auth.currentUser.photoURL
  └─ Tasks für diesen Benutzer laden
```

---

## 4. Funktionalität (Bestandsaufnahme)

### 4.1 Implementierte Features

✅ **Task Management**:
- Task erstellen (Title, Typ, Owner, Customer, Description, Tags)
- Task bearbeiten (Alle Felder)
- Task löschen
- Task archivieren/dearchivieren
- Task-Hierarchie (Column → SubColumn)

✅ **UI/UX**:
- Kanban Board mit Drag-and-Drop (Multiple Spalten/Unter-Spalten)
- Dark Mode / Light Mode (Theme-Toggle in Navbar)
- Responsive Design (Mobile, Tablet, Desktop)
- Visuelles Drag-Feedback (Erlaubte vs. Verbotene Drop-Zones)
- Details-Panel (Resizable Side-Panel für Task-Details)
- Multi-Select Filter (Task-Typen, Tags)
- Modale Dialoge (Task erstellen, Bestätigung, Konfiguration)
- Toast-Benachrichtigungen (Erfolg, Fehler)
- Custom Combobox/Dropdown-Komponenten

✅ **Persistierung**:
- Firestore als primärer Speicher
- LocalStorage als Fallback
- IndexedDB für Offline-Support

✅ **Authentifizierung**:
- Google OAuth Sign-In
- Anonymous/Guest Sign-In
- Logout
- Auth State Listener (UI Updates auf Auth-Changes)

### 4.2 Fehlende / Nicht Implementierte Features

- Keine Multi-User Echtzeitkollaboration (Real-time Listener)
- Keine Benutzerprofile/Verwaltung
- Keine Berechtigungssystem (ACL)
- Keine Revisionshistorie
- Keine Notifications/Alerts
- Keine API für externe Integrationen

---

## 5. Firestore-Struktur

### 5.1 Collections und Dokumente

**Collection: `tasks`**
```
tasks/{taskId} = {
  title: string,
  taskType: string,
  owner: string,
  customer: string,
  description: string,
  tags: array<string>,
  column: string (z.B. "TODO", "IN_PROGRESS", "DONE"),
  subColumn: string (z.B. "Planning", "Development", etc.),
  isArchived: boolean,
  createdAt: timestamp,
  updatedAt: timestamp,
  userId: string (Referenz zum Benutzer)
}
```

**Collection: `boardConfig`**
```
boardConfig/{docId} = {
  userId: string,
  columnOrder: array<string>,
  subColumnMapping: object { column → array<string> },
  filters: object { ...custom filters },
  ... weitere Konfiguration
}
```

### 5.2 Sicherheitsregeln

```firestore
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /tasks/{taskId} {
      allow read, write: if request.auth != null;
    }
    match /boardConfig/{docId} {
      allow read, write: if request.auth != null;
    }
    match /{document=**} {
      allow read, write: if false;
    }
  }
}
```

**Analyse**:
- ✅ Nur authentifizierte Benutzer
- ⚠️ **SICHERHEITSPROBLEME**:
  - Keine Validierung der taskId-Ownership
  - Jeder Benutzer kann jede Task bearbeiten/löschen
  - Keine Feldvalidierung
  - Keine Größenbeschränkungen

### 5.3 Firestore Indexes

**firestore.indexes.json**: Leer (keine Custom Indexes)

**Performance-Implikationen**:
- Default-Indexierung wird verwendet
- Keine Optimierung für komplexe Queries
- Für kleine Datasets (< 10.000 Tasks) ausreichend

---

## 6. Authentifizierung und Autorisierung

### 6.1 Aktuelle Implementierung

**Firebase Authentication**:
- Provider: Google OAuth + Anonymous
- Session-Verwaltung: Firebase SDK (Token im localStorage)
- UID-Zuordnung: `auth.currentUser.uid`

**Frontend-Prüfungen**:
- `onAuthStateChanged()` für State-Tracking
- UI-Elemente werden basierend auf Auth-Status angezeigt

**Backend-Sicherheit**:
- Nur `request.auth != null` in Firestore Rules

### 6.2 Sicherheitslücken

- ⚠️ Keine User-spezifische Datenscheidung (User A könnte User B's Tasks ändern)
- ⚠️ Keine Validierung auf Feldebene (z.B. Größe, Format)
- ⚠️ Keine Audit-Logs
- ⚠️ Anonymous Users haben volle Lese-/Schreib-Zugriff
- ⚠️ Keine Rate-Limiting

---

## 7. LocalStorage-Nutzung

**Identifizierte Keys**:
- `theme` - "dark" | "light"
- Potentiell weitere Tasks-Daten als Fallback

**Fallback-Szenario**:
```javascript
if (!firestore.accessible) {
  // Alle Operationen nutzen LocalStorage
  // Daten sind nur im Browser lokal verfügbar
  // Keine Synchronisierung zwischen Geräten
}
```

---

## 8. Migrationsrisiken und Herausforderungen

### 8.1 Kritische Risiken

| Risiko | Severity | Beschreibung |
|--------|----------|-------------|
| **Single-File SPA** | 🟠 HIGH | 135 KB monolithische HTML-Datei mit eingebettetem JS. Schwer zu modularisieren. |
| **Firebase-Abhängigkeiten** | 🔴 CRITICAL | Firebase SDK ist tief in der Code-Logik verankert. Muss vollständig durch REST-API ersetzt werden. |
| **Offline-Logic** | 🟠 HIGH | IndexedDB-Persistence muss durch SQLSync-Logik ersetzt werden. |
| **Auth-Flow** | 🟠 HIGH | Google OAuth Flow muss auf Windows Auth/ASP.NET Identity migriert werden. |
| **Firestore Queries** | 🟠 HIGH | Alle Firestore `getDocs()` / `getDoc()` Calls müssen zu REST API Calls werden. |
| **Echtzeit-Updates** | 🟡 MEDIUM | Keine Real-time Listener im aktuellen Code. Aber UI-Refresh-Muster müssen angepasst werden. |
| **Datenmigration** | 🟠 HIGH | Firestore Daten müssen zu SQLite migriert werden. Referentielle Integrität prüfen. |

### 8.2 Funktionale Risiken

- Drag-and-Drop muss nach API-Umzug getestet werden
- Dark Mode persisting muss gelöst sein
- Berechtigungen müssen serverseitig validiert werden

### 8.3 Performance-Risiken

- Single-File-Architektur könnte bei zu schnellem Wachstum problematisch werden
- Browser-Cache/Service-Worker für Offline nicht implementiert
- Keine Asset-Minification oder Code-Splitting

---

## 9. Code-Komplexität und Umfang

**Codezeilen (geschätzt)**:
- HTML/CSS: ~2000 Zeilen
- JavaScript-Logik: ~3000+ Zeilen
- Total: ~5000+ Zeilen in einer Datei

**JavaScript-Funktionen (identifiziert)**:
- `loginWithGoogle()` - Google OAuth
- `loginAsGuest()` - Anonymous Auth
- `logoutFromFirebase()` - Logout
- `loadTasks()` - Task-Collection laden
- `saveBoardConfig()` - Konfiguration speichern
- `createTask()` - Task erstellen (mit Modal)
- `editTask()` - Task bearbeiten
- `deleteTask()` - Task löschen
- `archiveTask()` - Task archivieren
- `updateTaskOrder()` - Drag-and-Drop Sortierung
- `applyFilters()` - Filter-Logik
- `toggleTheme()` - Dark Mode Toggle
- Weitere: 20+ Event-Handler und UI-Manipulationen

---

## 10. Abhängigkeitenliste

### 10.1 Frontend-CDN-Abhängigkeiten

```html
<script src="https://cdn.tailwindcss.com"></script>
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css">
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-app.js"></script>
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-firestore.js"></script>
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js"></script>
```

### 10.2 Backend-Abhängigkeiten (Ziel)

```
ASP.NET Core 10
Entity Framework Core 10
SQLite
Windows Authentication (oder ASP.NET Core Identity)
```

---

## 11. Empfohlene Migrations-Roadmap

### Phase 1: ✅ Bestandsaufnahme (ABGESCHLOSSEN)
- [x] Repository klonen
- [x] Struktur analysieren
- [x] Firebase-Abhängigkeiten dokumentieren
- [x] Datenflüsse analysieren
- [x] Risiken identifizieren

### Phase 2: Zielarchitektur-Design
- [ ] Backend-Projektstruktur definieren
- [ ] Database-Schema erstellen
- [ ] REST API Endpoints definieren
- [ ] Authentifizierungs-Flow definieren
- [ ] Synchronisierungs-Konzept entwerfen

### Phase 3: ASP.NET Core Backend
- [ ] Solution mit Schichtenarchitektur erstellen
- [ ] EF Core Models definieren
- [ ] SQLite-Datenbank initialisieren
- [ ] REST API implementieren
- [ ] Authentifizierung konfigurieren
- [ ] Unit Tests schreiben

### Phase 4: Frontend-Migration
- [ ] JavaScript API-Client erstellen
- [ ] Firebase-Imports entfernen
- [ ] Alle Firebase-Calls durch REST-API Calls ersetzen
- [ ] Authentifizierungs-Flow anpassen
- [ ] Offline-Logik anpassen (ggf. Service Worker)
- [ ] Integration Tests durchführen

### Phase 5: Datenmigration
- [ ] Export-Tool für Firestore implementieren
- [ ] Import-Tool für SQLite implementieren
- [ ] Datenintegrität validieren
- [ ] Rollback-Verfahren testen

### Phase 6: IIS-Deployment
- [ ] Publish-Profil erstellen
- [ ] IIS-Konfiguration durchführen
- [ ] HTTPS/TLS konfigurieren
- [ ] Backup-Strategien implementieren
- [ ] Monitoring einrichten

### Phase 7: Firebase entfernen
- [ ] Firebase SDK entfernen
- [ ] Firebase-Konfiguration löschen
- [ ] Firebase-Rules archivieren
- [ ] Test-Akzeptanz durchführen

---

## 12. Metriken und Richtlinien

| Metrik | Wert | Hinweis |
|--------|------|--------|
| Dateien zu migrieren | 1 | Monolithische index.html |
| Firebase-Abhängigkeiten | 3 | App, Firestore, Auth SDKs |
| Zu ersetzende Funktionen | 25+ | Firebase Calls, Auth, CRUD |
| Geschätzte Komplexität | 🟠 HIGH | Eng verwobener Firebase-Code |
| Sicherheits-Aufwand | 🟠 HIGH | Neue ACL & Validierung erforderlich |
| Datenmigrations-Aufwand | 🟡 MEDIUM | ~1000+ Tasks erwartet (Schätzung) |
| Testing-Aufwand | 🔴 CRITICAL | Vollständige Regression erforderlich |

---

## 13. Arbeitsaufwand-Schätzung

| Phase | Aufwand | Risiko |
|-------|---------|--------|
| Phase 2: Design | 8-16 Stunden | Mittel |
| Phase 3: Backend | 40-60 Stunden | Hoch |
| Phase 4: Frontend | 30-50 Stunden | Hoch |
| Phase 5: Migration | 10-20 Stunden | Mittel |
| Phase 6: Deployment | 10-20 Stunden | Mittel |
| Phase 7: Cleanup | 5-10 Stunden | Niedrig |
| **Total** | **113-176 Stunden** | **Hoch** |

---

## 14. Nächste Schritte

Nach Bestätigung dieser Analyse:

1. **Phase 2 starten**: Zielarchitektur detailliert entwerfen
2. **Entscheidungen treffen**:
   - Windows Auth vs. ASP.NET Core Identity?
   - SQLite Datenbankpfad? (Empfehlung: `D:\Apps\prKanban\Data\prkanban.db`)
   - Sollte offline-Sync unterstützt werden?
   - Multi-Tenant oder Single-Project?

3. **Dokumentation erstellen**:
   - ARCHITECTURE.md
   - DATABASE.md
   - API.md
   - AUTHENTICATION.md

---

## 15. Anhang: Dateigröße und Komplexität

```
index.html .......................... 135 KB
├── HTML Structure ................. ~40 KB
├── CSS (Tailwind) ................. ~20 KB
├── JavaScript (Anwendung) ......... ~75 KB
└── Firebase Konfiguration ......... <1 KB

Projekt Total (ohne .agents/) ....... ~200 KB
Projekt Total (mit .agents/) ....... ~2 MB (externe Dokumentation)
```

---

**Status**: ✅ Phase 1 abgeschlossen und zur Freigabe bereit.

**Dokumentation**: `C:\prKanban-migration\docs\MIGRATION_ANALYSIS.md`  
**Repository**: `C:\prKanban-migration` (Branch: `feature/migrate-firebase-to-sqlite`)  
**Originalprojekt**: Unverändert auf `C:\prKanban`
