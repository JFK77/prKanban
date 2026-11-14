# 🔐 prKanban Login Erklärung

## Überblick: Wie funktioniert das Login?

Das prKanban Login arbeitet mit einem **REST API Backend** (ASP.NET Core) und **httpOnly Cookies** für sichere Authentifizierung.

---

## 🔄 Login-Ablauf (Schritt für Schritt)

### 1️⃣ User klickt "Login" Button
```
Browser                          Frontend
  │                                │
  └─→ Click "Login" Button ────→   loginWithGoogle()
                                    │
                                    ├─→ prompt('Email:')
                                    ├─→ prompt('Password:')
                                    │
                                    └─→ AuthAPI.login(email, password)
```

### 2️⃣ API Client sendet Credentials
```javascript
// frontend (api-client.js)
async login(email, password) {
  const response = await fetch('http://localhost:5000/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',  // ← WICHTIG: Cookies mitschicken!
    body: JSON.stringify({
      email,
      password
    })
  });
  
  return await response.json();
}
```

### 3️⃣ Backend validiert Credentials
```
Frontend                          Backend (ASP.NET Core)
  │                                  │
  ├─→ POST /api/v1/auth/login ────→  AuthController.Login()
                                      │
                                      ├─→ Find user in database
                                      │   (AspNetUsers table)
                                      │
                                      ├─→ Check password hash
                                      │   (ASP.NET Identity)
                                      │
                                      ├─→ Validate credentials
                                      │   ✅ Password korrekt?
                                      │   ✅ Account aktiv?
                                      │
                                      └─→ Create session + Cookie
```

### 4️⃣ Backend setzt httpOnly Cookie
```
Backend                              Frontend (Browser)
  │                                     │
  └─→ HTTP Response ─────────────────→  Browser
      {
        "success": true,
        "data": {
          "id": "user-123",
          "email": "user@example.com",
          "fullName": "Test User"
        }
      }
      
      Set-Cookie: .AspNetCore.Identity.Application=xxxxx
                  HttpOnly; SameSite=Strict; Secure
                  │
                  └─→ Cookie wird im Browser gespeichert!
                      (Automatisch bei jedem Request mitgesendet)
```

### 5️⃣ Frontend verarbeitet Login-Response
```javascript
// index.html
try {
  const user = await AuthAPI.login(email, password);
  currentUser = user;                    // User speichern
  updateConnectionUI();                  // UI aktualisieren
  await loadBoardConfigFromCloud();      // Board laden
  await loadTasksFromCloud();            // Tasks laden
  showToast('Erfolgreich angemeldet!');
} catch (error) {
  showToast('Login fehlgeschlagen: ' + error.message);
}
```

### 6️⃣ Benutzer ist jetzt angemeldet! ✅
```
User kann jetzt:
  ✅ Tasks erstellen
  ✅ Tasks aktualisieren
  ✅ Tasks löschen
  ✅ Board anschauen
  ✅ Tasks filtern
  
Die Session wird durch den httpOnly Cookie aufrechterhalten!
```

---

## 📊 Detaillierter Ablauf (Technisch)

### Login-Sequenz-Diagramm

```
┌────────────┐                  ┌──────────────┐              ┌────────────┐
│  Browser   │                  │   Frontend   │              │  Backend   │
│            │                  │  (HTML/JS)   │              │ (ASP.NET)  │
└────────────┘                  └──────────────┘              └────────────┘
      │                               │                            │
      │ 1. Click "Login"              │                            │
      ├──────────────────────────────→│                            │
      │                               │                            │
      │                        prompt() Dialog                      │
      │←──────────────────────────────│                            │
      │ Email: user@test.dev          │                            │
      │ Password: ••••••              │                            │
      │                               │                            │
      │ 2. OK Button                  │                            │
      ├──────────────────────────────→│                            │
      │                               │                            │
      │                        3. POST /api/v1/auth/login          │
      │                        {email, password}                   │
      │                               ├──────────────────────────→│
      │                               │                            │
      │                               │        4. Find User in DB  │
      │                               │        SELECT * FROM       │
      │                               │        AspNetUsers         │
      │                               │        WHERE Email = ...   │
      │                               │                            │
      │                               │    5. Check Password Hash  │
      │                               │    if (PasswordHash        │
      │                               │      == Hash(password))    │
      │                               │                            │
      │                               │    6. Password CORRECT ✅   │
      │                               │                            │
      │                               │    7. Create Session       │
      │                               │    Set httpOnly Cookie     │
      │                               │                            │
      │                        8. HTTP 200 OK                      │
      │                        Set-Cookie: ...                     │
      │                        {success: true,                     │
      │←──────────────────────────────┤ data: {id, email, ...}}   │←
      │                               │                            │
      │    9. Browser speichert Cookie│                            │
      │    automatisch!               │                            │
      │                               │                            │
      │    10. Frontend speichert User│                            │
      │    currentUser = user         │                            │
      │                               │                            │
      │    11. Board laden            │                            │
      │    GET /api/v1/tasks         │                            │
      │    (Cookie wird automatisch   │                            │
      │     mitgesendet!)             │                            │
      │                               ├──────────────────────────→│
      │                               │                            │
      │                               │    12. Cookie validieren   │
      │                               │    → Request authentifiziert
      │                               │                            │
      │                               │    13. Benutzer-Tasks      │
      │                               │    SELECT * FROM Tasks     │
      │                               │    WHERE UserId = ...      │
      │                               │                            │
      │                        14. HTTP 200 + Tasks                │
      │←──────────────────────────────┤←───────────────────────────│
      │                               │                            │
      │    15. Board anzeigen         │                            │
      │                               │                            │
      └────────────┘                  └──────────────┘              └────────────┘
```

---

## 🔑 httpOnly Cookie (Das Wichtigste!)

### Was ist ein httpOnly Cookie?

```
Cookie = Kleine Datei, die Browser speichert
Wird automatisch bei jedem Request zum Server mitgesendet
```

### Warum httpOnly Cookie?

```
❌ Alte Methode (localStorage):
   - localStorage.setItem('token', 'xyz123')
   - Vulnerable für XSS-Attacken!
   - JavaScript kann Token stehlen

✅ httpOnly Cookie:
   - Kann NUR vom Server gesetzt werden
   - JavaScript kann NICHT darauf zugreifen
   - Wird automatisch mitgesendet
   - Geschützt vor XSS!
```

### Wie funktioniert es?

```javascript
// BACKEND setzt Cookie:
Set-Cookie: .AspNetCore.Identity.Application=abc123def456
            HttpOnly          // ← JavaScript kann nicht zugreifen!
            Secure            // ← Nur über HTTPS
            SameSite=Strict   // ← Nur same-origin Requests

// FRONTEND sendet Request:
fetch('http://localhost:5000/api/v1/tasks', {
  credentials: 'include'  // ← "Bitte Cookie mitschicken!"
})

// Browser macht automatisch:
GET /api/v1/tasks
Cookie: .AspNetCore.Identity.Application=abc123def456
        ↑ Automatisch mitgesendet!
```

---

## 🧪 Login-Beispiel (Praktisch)

### Szenario: User login@test.dev möchte einloggen

**Schritt 1: User klickt Login**
```
Frontend zeigt:
  Email: [  login@test.dev  ]
  Passwort: [  •••••••  ]
```

**Schritt 2: Frontend sendet:**
```json
POST http://localhost:5000/api/v1/auth/login
{
  "email": "login@test.dev",
  "password": "SecurePass123!"
}
```

**Schritt 3: Backend überprüft:**
```
1. Finde Benutzer in Datenbank
   SELECT * FROM AspNetUsers WHERE Email = 'login@test.dev'
   
   RESULT:
   ├─ Id: "user-12345"
   ├─ Email: "login@test.dev"
   ├─ PasswordHash: "$2a$11$N9qo8uLO..."  (verschlüsselt!)
   └─ EmailConfirmed: true

2. Überprüfe Passwort
   Hash("SecurePass123!") == "$2a$11$N9qo8uLO..."
   
   ✅ MATCH! Passwort korrekt!

3. Erstelle Session + Cookie
   Set-Cookie: .AspNetCore.Identity.Application=XyZ789
```

**Schritt 4: Backend antwortet:**
```json
HTTP 200 OK
Set-Cookie: .AspNetCore.Identity.Application=XyZ789...

{
  "success": true,
  "data": {
    "id": "user-12345",
    "email": "login@test.dev",
    "fullName": "Login User"
  }
}
```

**Schritt 5: Frontend:**
```javascript
currentUser = {
  id: "user-12345",
  email: "login@test.dev",
  fullName: "Login User"
}

// Board wird angezeigt
// Browser sendet Cookie automatisch bei allen Requests
```

**Schritt 6: Benutzer kann arbeiten!**
```
✅ GET /api/v1/tasks          → Cookie mitgesendet → ✅ Authentifiziert
✅ POST /api/v1/tasks         → Cookie mitgesendet → ✅ Authentifiziert
✅ PUT /api/v1/tasks/{id}     → Cookie mitgesendet → ✅ Authentifiziert
✅ DELETE /api/v1/tasks/{id}  → Cookie mitgesendet → ✅ Authentifiziert
```

---

## 👻 Gast-Login (Guest Login)

### Wie funktioniert Gast-Login?

```javascript
// Benutzer klickt "Gast anmelden"

window.loginAsGuest = async function() {
  // 1. Erzeuge eindeutige Email
  const email = 'guest-' + Date.now() + '@local.dev'
               // guest-1731488700000@local.dev
  
  // 2. Verwende vordefiniertes Passwort
  const password = 'GuestPass123!'
  
  // 3. Registriere Gast-Account
  await AuthAPI.register(email, password)
  
  // 4. Logge Gast automatisch ein
  const user = await AuthAPI.login(email, password)
  
  // 5. Fertig! Gast ist angemeldet
  currentUser = user
}
```

### Ablauf:

```
1️⃣ Click "Gast anmelden"
          ↓
2️⃣ Generiere guest-12345@local.dev
          ↓
3️⃣ Registriere neuen Benutzer
   POST /api/v1/auth/register
   {
     email: "guest-12345@local.dev",
     password: "GuestPass123!",
     confirmPassword: "GuestPass123!",
     fullName: "Guest User"
   }
          ↓
4️⃣ Backend erstellt Account in AspNetUsers
          ↓
5️⃣ Automatisch einloggen
   POST /api/v1/auth/login
   {
     email: "guest-12345@local.dev",
     password: "GuestPass123!"
   }
          ↓
6️⃣ Backend setzt Cookie
   Set-Cookie: .AspNetCore.Identity.Application=...
          ↓
7️⃣ Frontend speichert currentUser
          ↓
8️⃣ Gast kann Board nutzen!
   (Nur seine eigenen Tasks sehen)
```

---

## 🔐 Sicherheit: Passwort-Hashing

### Wie werden Passwörter gespeichert?

```
Benutzer gibt ein: "SecurePass123!"
                   ↓
Backend hasht es:  Hash("SecurePass123!")
                   ↓
Gespeichert in DB: "$2a$11$N9qo8uLOickgx2ZMRZoMyez..."
                   (NICHT das eigentliche Passwort!)
```

### Bei Login:

```
Benutzer gibt ein: "SecurePass123!"
                   ↓
Backend hasht es:  Hash("SecurePass123!")
                   ↓
Vergleich:         Hash("SecurePass123!") 
                   ==
                   "$2a$11$N9qo8uLOickgx2ZMRZoMyez..."
                   ↓
                   ✅ MATCH → Login erfolgreich
                   ❌ NO MATCH → Login fehlgeschlagen
```

**Wichtig**: Das originale Passwort wird NIEMALS gespeichert!

---

## 📱 Logout

### Was passiert beim Logout?

```javascript
window.logoutFromFirebase = async function() {
  // 1. Sende Logout-Request
  await AuthAPI.logout()
  // POST /api/v1/auth/logout
  
  // 2. Backend löscht Cookie
  // (setzt Cookie-Expiration auf Vergangenheit)
  
  // 3. Frontend löscht lokale User-Info
  currentUser = null
  
  // 4. Board wird geleert
  tasks = []
  
  // 5. UI aktualisiert
  // Login-Button wird wieder angezeigt
}
```

**Nach Logout:**
```
- httpOnly Cookie ist gelöscht ❌
- currentUser = null ❌
- Tasks werden nicht mehr angezeigt ❌
- Benutzer muss sich erneut anmelden
```

---

## 🔄 Authentifizierung bei Seitenaktualisierung

### Was passiert, wenn User F5 drückt?

```
1. Browser wird neu geladen
2. index.html wird geladen
3. JavaScript wird ausgeführt
4. DOMContentLoaded-Event
   → initAuth() wird aufgerufen
   
5. initAuth() macht:
   const user = await AuthAPI.getCurrentUser()
   // GET /api/v1/auth/user
   
6. Browser sendet httpOnly Cookie automatisch!
   GET /api/v1/auth/user
   Cookie: .AspNetCore.Identity.Application=...
   
7. Backend überprüft Cookie
   ✅ Cookie gültig?
      → Return current user info
   ❌ Cookie ungültig/abgelaufen?
      → Return 401 Unauthorized
      
8. Frontend:
   ✅ currentUser = user (aus Datenbank)
      → Tasks werden geladen
      → Board wird angezeigt
      → Session persistiert!
      
   ❌ currentUser = null
      → Benutzer muss einloggen
```

**Ergebnis**: Session bleibt bestehen auch nach Seitenaktualisierung! ✅

---

## 📊 Datenbank: AspNetUsers Tabelle

### Was wird gespeichert?

```
AspNetUsers (Tabelle)
├─ Id: "user-12345"
├─ Email: "user@test.dev"
├─ NormalizedEmail: "USER@TEST.DEV"
├─ EmailConfirmed: true/false
├─ PasswordHash: "$2a$11$N9qo8uLO..." (verschlüsselt)
├─ SecurityStamp: "..."
├─ PhoneNumber: null
├─ PhoneNumberConfirmed: false
├─ TwoFactorEnabled: false
├─ LockoutEnd: null
├─ LockoutEnabled: true
├─ AccessFailedCount: 0
└─ UserName: "user@test.dev"
```

### Beispiel-Datensatz:

```
user-12345 | user@test.dev | USER@TEST.DEV | $2a$11$N9q... | ...
guest-1731488700000 | guest-1731488700000@local.dev | ... | $2a$11$... | ...
```

---

## 🎯 Zusammenfassung

### Login funktioniert so:

1. **User gibt Email + Passwort ein** (prompt() Dialog)
2. **Frontend sendet zu Backend** (POST /auth/login)
3. **Backend validiert Credentials** (Check in AspNetUsers DB)
4. **Backend setzt httpOnly Cookie** (Sichere Session)
5. **Frontend speichert User-Info** (currentUser Variable)
6. **Board wird geladen** (GET /tasks mit Cookie)
7. **Benutzer kann arbeiten** (Alle Requests haben Cookie)

### Sicherheit:

✅ Passwörter werden gehasht (nicht im Klartext)  
✅ httpOnly Cookies (XSS-Schutz)  
✅ SameSite Policy (CSRF-Schutz)  
✅ Benutzer-Isolation (nur eigene Tasks)  
✅ Session-Validierung bei jedem Request  

### Session bleibt bei:
✅ Seitenaktualisierung (F5)  
✅ Neuer Tab  
✅ Browser Neustart (sofern nicht private mode)  

### Session endet bei:
❌ Logout (Cookie gelöscht)  
❌ Cookie-Expiration  
❌ Browser Cache gelöscht  

---

## 🧪 Live Test

Du kannst das Login selbst testen:

```
1. Öffne http://localhost:3000
2. Klicke "Login" Button
3. Gib Email + Passwort ein
4. Backend validiert
5. httpOnly Cookie wird gesetzt
6. Board wird angezeigt
```

Oder Gast-Login:
```
1. Öffne http://localhost:3000
2. Klicke "Gast anmelden"
3. Neue Gast-Account wird erstellt
4. Automatisch eingeloggt
5. Board wird angezeigt
```

---

**Login ist jetzt vollständig verstanden!** 🎉
