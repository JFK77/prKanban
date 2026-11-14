# 🎨 Login Prozess - Visuelle Anleitung

## Übersicht: Die 3 Login-Methoden

```
┌─────────────────────────────────────────────────────────────────────┐
│                     PRKANBAN LOGIN OPTIONS                          │
└─────────────────────────────────────────────────────────────────────┘

    ┌─────────────────────┐         ┌──────────────────────┐
    │   LOGIN BUTTON      │         │   GUEST BUTTON       │
    │  "Normaler Login"   │         │  "Gast anmelden"     │
    └──────────┬──────────┘         └──────────┬───────────┘
               │                               │
               ├─→ Email eintippen            ├─→ Auto-Email erzeugen
               ├─→ Passwort eintippen        ├─→ Auto-Passwort
               ├─→ An Backend senden         ├─→ Registrieren
               └─→ Cookie setzen             └─→ Einloggen + Cookie
```

---

## 🔑 Normaler Login (Step-by-Step)

### Schritt 1: Button klicken
```
╔═══════════════════════════════════════════════════════╗
║  Dynamic Workflow Kanban                              ║
║                                                       ║
║    [ Login ]  [ Gast ]  [⊙ Menu]                     ║
║                ↑                                      ║
║          User klickt hier                            ║
╚═══════════════════════════════════════════════════════╝
```

### Schritt 2: Prompt Dialog (Eingabe)
```
┌─────────────────────────────────────┐
│  Browser JavaScript Prompt         │
├─────────────────────────────────────┤
│                                     │
│  Email:                             │
│  ┌───────────────────────────────┐  │
│  │ user@test.dev               │  │
│  └───────────────────────────────┘  │
│                                     │
│       [ OK ]  [ Cancel ]            │
│                                     │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│  Browser JavaScript Prompt          │
├─────────────────────────────────────┤
│                                     │
│  Password:                          │
│  ┌───────────────────────────────┐  │
│  │ ••••••••••                   │  │
│  └───────────────────────────────┘  │
│                                     │
│       [ OK ]  [ Cancel ]            │
│                                     │
└─────────────────────────────────────┘
```

### Schritt 3: Frontend sendet Request
```
Frontend (JavaScript)                Backend (ASP.NET Core)
─────────────────────               ──────────────────────

await fetch(                            
  'http://localhost:5000/api/v1/auth/login',
  {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',    ← Wichtig!
    body: JSON.stringify({
      email: 'user@test.dev',
      password: 'SecurePass123!'
    })
  }
)
    │
    │─ HTTP POST Request →─────────────────────────────→
    │                                                      │
    │                                              AuthController
    │                                                      │
    │                                              ├─ Find User
    │                                              ├─ Check Password
    │                                              ├─ Validate
    │                                              └─ Create Cookie
    │
    │← HTTP 200 Response ←──────────────────────────────
    │  Set-Cookie: ...
    │  { success: true, data: { ... } }
```

### Schritt 4: Browser speichert Cookie
```
HTTP Response Header:
┌─────────────────────────────────────────────────────────┐
│ Set-Cookie: .AspNetCore.Identity.Application=abc123  │
│             HttpOnly; SameSite=Strict; Secure        │
└─────────────────────────────────────────────────────────┘
                         │
                         ↓
            Browser Cookie Storage
        ┌─────────────────────────────┐
        │ Cookie: abc123              │
        │ (wird automatisch bei       │
        │  jedem Request mitgesendet) │
        └─────────────────────────────┘
```

### Schritt 5: Frontend verarbeitet Response
```javascript
Response:
{
  success: true,
  data: {
    id: "user-12345",
    email: "user@test.dev",
    fullName: "Test User"
  }
}

Frontend:
  ├─ currentUser = { id: ..., email: ... }  ✅
  ├─ updateConnectionUI()                    ✅
  ├─ loadBoardConfigFromCloud()              ✅
  ├─ loadTasksFromCloud()                    ✅
  └─ showToast('Erfolgreich angemeldet!')    ✅
```

### Schritt 6: Board wird angezeigt
```
┌────────────────────────────────────────────────────┐
│ User: user@test.dev  |  API: REST (ASP.NET Core)  │
├────────────────────────────────────────────────────┤
│                                                    │
│  [ Backlog ]  [ Todo ]  [ In Progress ]  [ Done ] │
│     │            │            │             │     │
│  ┌──────┐   ┌────────┐  ┌──────────┐  ┌────────┐ │
│  │Task 1│   │Task 2  │  │  Task 3  │  │Task 4  │ │
│  │      │   │        │  │          │  │        │ │
│  │ ...  │   │ ...    │  │  ...     │  │  ...   │ │
│  └──────┘   └────────┘  └──────────┘  └────────┘ │
│     │            │            │             │     │
│  ┌──────┐   ┌────────┐  ┌──────────┐  ┌────────┐ │
│  │Task 5│   │Task 6  │  │  Task 7  │  │Task 8  │ │
│  │      │   │        │  │          │  │        │ │
│  │ ...  │   │ ...    │  │  ...     │  │  ...   │ │
│  └──────┘   └────────┘  └──────────┘  └────────┘ │
│                                                    │
│  [ + Add Task ]  [ Logout ]                       │
└────────────────────────────────────────────────────┘
```

---

## 👻 Gast-Login (Guest Login)

### Automatischer Prozess
```
User klickt "Gast anmelden"
           │
           ↓
    ┌─────────────────────┐
    │ Keine Eingabe nötig!│
    └─────────────────────┘
           │
           ↓
    Frontend erzeugt:
    ├─ email = 'guest-1731488700000@local.dev'
    ├─ password = 'GuestPass123!'
    └─ fullName = 'Guest User'
           │
           ↓
    ┌──────────────────────────┐
    │ Registrierung           │
    │ POST /auth/register     │
    │ { email, password, ... }│
    └──────────┬───────────────┘
               │
               ↓
    Backend erstellt neuen Benutzer
    ├─ Speichert in AspNetUsers
    ├─ Passwort wird gehasht
    └─ Account ready ✅
               │
               ↓
    ┌──────────────────────────┐
    │ Automatisches Login     │
    │ POST /auth/login        │
    │ { email, password }     │
    └──────────┬───────────────┘
               │
               ↓
    Backend validiert & setzt Cookie
               │
               ↓
    Frontend:
    ├─ currentUser = guest info
    ├─ loadTasksFromCloud()
    ├─ Board wird angezeigt
    └─ Toast: "Als Gast angemeldet!"
               │
               ↓
    ✅ Gast kann Board nutzen!
```

### Beispiel-Gast-Account
```
Generierte Daten:
├─ Email: guest-1731488700000@local.dev
├─ Password: GuestPass123!
├─ Name: Guest User
└─ ID: guest-uuid-12345

In Datenbank gespeichert:
┌──────────────────────────────────────────────┐
│ AspNetUsers                                  │
├──────────────┬──────────────────────┬────────┤
│ Id           │ Email                │ Type   │
├──────────────┼──────────────────────┼────────┤
│ guest-12345  │ guest-1731488700000  │ guest  │
│              │ @local.dev           │        │
└──────────────┴──────────────────────┴────────┘

Session:
├─ Cookie gesetzt: httpOnly Cookie
├─ currentUser: { id: guest-12345, email: ... }
└─ Tasks: Nur Gast-Tasks sichtbar
```

---

## 🔐 Was passiert hinter den Kulissen?

### 1️⃣ Passwort-Hashing (Sicherheit)
```
User-Input:          "SecurePass123!"
                            │
                            ↓
                   Hash-Algorithmus
                   (ASP.NET Identity)
                            │
                            ↓
Gespeichert in DB:   "$2a$11$N9qo8uLO..."
(Nicht lesbar!)

Bei Login:
Input "SecurePass123!"  →  Hash  →  "$2a$11$N9qo8uLO..."
                                            │
                                            ↓
                                    Vergleich mit DB
                                            │
                                  ✅ Match → Login OK
                                  ❌ No Match → Login Fail
```

### 2️⃣ httpOnly Cookie (Sicherheit)
```
Backend setzt:
┌──────────────────────────────────────────────────┐
│ Set-Cookie: .AspNetCore.Identity.Application=...│
│             HttpOnly;                            │
│             Secure;                              │
│             SameSite=Strict;                     │
│             Path=/;                              │
│             Expires=...                          │
└──────────────────────────────────────────────────┘

Bedeutung:
├─ HttpOnly: JavaScript KANN NICHT zugreifen ✅ (XSS-Schutz)
├─ Secure: Nur über HTTPS ✅
├─ SameSite=Strict: Nur same-origin ✅ (CSRF-Schutz)
├─ Path=/: Für alle Routes gültig ✅
└─ Expires: Ablaufdatum ✅

Browser macht automatisch:
├─ Speichert Cookie
├─ Sendet mit JEDEM Request
├─ Mit Ablaufdatum prüfen
└─ Löscht nach Expiration
```

### 3️⃣ Session-Validierung
```
Jeder Request zum Backend:

┌─────────────────────────────────┐
│ Frontend sendet Request         │
├─────────────────────────────────┤
│ GET /api/v1/tasks               │
│ Cookie: .AspNetCore.Id=abc123   │
│         (automatisch!)          │
└────────────┬────────────────────┘
             │
             ↓
┌─────────────────────────────────┐
│ Backend verarbeitet Request     │
├─────────────────────────────────┤
│ 1. Lese Cookie aus Request      │
│ 2. Validiere Cookie             │
│ 3. Finde User in Database       │
│ 4. Überprüfe Berechtigungen     │
│                                 │
│ ✅ Alles OK?                    │
│    → Bearbeite Request          │
│    → Sende Tasks zurück         │
│                                 │
│ ❌ Cookie ungültig?             │
│    → Return 401 Unauthorized    │
│    → Cookie löschen             │
└─────────────────────────────────┘
```

---

## 📊 Datenbank-Perspektive

### AspNetUsers Tabelle
```
┌──────────────┬──────────────────────┬────────────────────┐
│ Id           │ Email                │ PasswordHash       │
├──────────────┼──────────────────────┼────────────────────┤
│ user-123     │ user@test.dev        │ $2a$11$N9qo8uLO... │
│ guest-456    │ guest-1731488700000@ │ $2a$11$XyZ7890... │
│              │ local.dev            │                    │
│ admin-789    │ admin@test.dev       │ $2a$11$AbC4567... │
└──────────────┴──────────────────────┴────────────────────┘

Eigenschaften:
├─ Id: Eindeutige Benutzer-ID (UUID)
├─ Email: Login-Email (eindeutig)
├─ PasswordHash: Verschlüsseltes Passwort
│  (Nicht das eigentliche Passwort!)
│
└─ Zusätzliche Felder:
   ├─ EmailConfirmed
   ├─ PhoneNumber
   ├─ LockoutEnd
   ├─ AccessFailedCount
   └─ ...
```

### Tasks Tabelle (Benutzer-spezifisch)
```
┌──────────────┬──────────────┬─────────────────────┐
│ Id           │ UserId       │ Title               │
├──────────────┼──────────────┼─────────────────────┤
│ task-001     │ user-123     │ Neue Feature        │
│ task-002     │ user-123     │ Bug beheben         │
│ task-003     │ guest-456    │ Test-Task           │
│ task-004     │ admin-789    │ System-Wartung      │
└──────────────┴──────────────┴─────────────────────┘

Wichtig:
├─ Jeder Task gehört zu einem UserId
├─ Query: SELECT * FROM Tasks WHERE UserId = 'user-123'
└─ Benutzer sehen nur ihre eigenen Tasks!
```

---

## 🔄 Session-Persistierung

### Nach Seitenaktualisierung (F5)

```
┌──────────────────────────────────┐
│ Browser Refresh (F5)             │
├──────────────────────────────────┤
│                                  │
│ 1. HTML wird neu geladen         │
│    <script> wird ausgeführt      │
│                                  │
│ 2. DOMContentLoaded Event        │
│    → initAuth() aufgerufen       │
│                                  │
│ 3. initAuth() macht:             │
│    const user =                  │
│      await AuthAPI.getCurrentUser()
│                                  │
│    GET /api/v1/auth/user         │
│    Cookie: (automatisch!)        │
│                                  │
│ 4. Backend validiert Cookie      │
│    ✅ Cookie gültig?             │
│       → Return current User      │
│    ❌ Cookie ungültig?           │
│       → Return 401               │
│                                  │
│ 5. Frontend:                     │
│    ✅ currentUser = user         │
│       → Board wird geladen       │
│       → Tasks werden angezeigt   │
│       → Session bleibt bestehen! │
│                                  │
│    ❌ currentUser = null         │
│       → Board ist leer           │
│       → Login-Button angezeigt   │
│       → Benutzer muss neu loggen │
│                                  │
└──────────────────────────────────┘
```

### Zeitlinie einer Session

```
10:00 - Login ✅ (Cookie gesetzt)
         │
         ├─ Board wird geladen
         ├─ Tasks angezeigt
         └─ Cookie in Browser speichert

10:15 - F5 Refresh
         │
         ├─ Frontend sendet GET /auth/user
         ├─ Backend validiert Cookie
         ├─ Cookie gültig! ✅
         └─ Session bleibt aktiv

10:30 - Logout
         │
         ├─ POST /auth/logout
         ├─ Backend löscht Cookie
         ├─ Browser entfernt Cookie
         └─ currentUser = null

10:31 - Versuche Tasks zu laden
         │
         ├─ GET /api/v1/tasks
         ├─ Keine Cookie mitgesendet
         ├─ Backend: 401 Unauthorized
         └─ Login-Seite wird angezeigt

10:35 - Erneuter Login
         │
         ├─ POST /auth/login
         ├─ Backend validiert Credentials
         ├─ Neue Cookie gesetzt
         └─ Session neu gestartet ✅
```

---

## 🎯 Login-Flow Zusammenfassung

```
┌─────────────────────────────────────────────────────────────┐
│                    LOGIN PROCESS                            │
└─────────────────────────────────────────────────────────────┘

                      Frontend                Backend
                        ┌──┐              ┌────────────────┐
                        │  │              │   ASP.NET      │
                        │  │              │   Core API     │
                        └──┘              └────────────────┘
                        │                        │
   User klickt Login    │                        │
           │            │                        │
           └──→ Frontend │                        │
                        │                        │
              Prompt für │                        │
              Email+Pass │                        │
                        │                        │
              Sende POST │                        │
                        ├──→ POST /auth/login ──→│
                        │                        │
                        │                   Validierung:
                        │                   • Find User
                        │                   • Check Password
                        │                   • Create Session
                        │                   • Set Cookie
                        │                        │
                        │← HTTP 200 OK ─────────┤
                        │  Set-Cookie: ...       │
                        │  { data: user }        │
                        │                        │
              Speichere │                        │
              currentUser│                        │
                        │                        │
              Lade Tasks │                        │
                        ├──→ GET /api/v1/tasks →│
                        │    (+ Cookie)          │
                        │                   Gib Tasks
                        │← HTTP 200 ────────────┤
                        │  { tasks: [...] }      │
                        │                        │
              Zeige Board │                        │
                        │                        │
           ✅ Login OK! │                        │

Ergebnis:
├─ currentUser = Benutzer-Info ✅
├─ httpOnly Cookie = Session ✅
├─ Tasks = Benutzer-Tasks ✅
├─ Board = Sichtbar ✅
└─ Weitere Requests = Mit Cookie ✅
```

---

## 🚀 Du bist jetzt Login-Expert! 

Merke dir:
1. **Login** = Email + Passwort → Backend validiert
2. **Cookie** = Browser speichert automatisch
3. **Session** = Bleibt über Seitenaktualisierung
4. **Gast** = Auto-generierte Email + Passwort
5. **Sicherheit** = Passwort-Hashing + httpOnly Cookie
6. **Logout** = Cookie wird gelöscht

**Fragen?** Schau dir `LOGIN_FLOW_EXPLANATION.md` an!
