# Phase 4: Frontend Migration Plan
## Firebase → ASP.NET Core REST API

### Ausgangssituation

**Frontend-Struktur:**
- Vanilla HTML/CSS/JavaScript (keine React, Vue, Angular)
- Alles in einer HTML-Datei: `public/index.html`
- Firebase SDK direkt von CDN
- Keine echte State Management Library
- Vanilla localStorage für Theme/Snapshots
- Tailwind CSS + Font Awesome von CDN

**Aktueller Data Flow:**
```
UI (HTML) → Firebase SDK → Firestore
              ↓
         Authentication (Google/Anonymous)
```

**Zielzustand (Phase 4):**
```
UI (HTML) → REST API Client → ASP.NET Core Backend
              ↓
         Authentication (httpOnly Cookies)
```

---

## Migrationsschritte

### **Schritt 1: REST API Client erstellen**
**Datei:** `public/api-client.js`

```javascript
// API Base URL
const API_BASE = 'http://localhost:5000/api/v1';

// State für httpOnly Cookies
let authToken = null;

// Authentication API
const AuthAPI = {
  async register(email, password, fullName) {
    return fetch(`${API_BASE}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include', // httpOnly Cookies
      body: JSON.stringify({ email, password, fullName })
    }).then(r => r.json());
  },

  async login(email, password) {
    return fetch(`${API_BASE}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify({ email, password })
    }).then(r => r.json());
  },

  async logout() {
    return fetch(`${API_BASE}/auth/logout`, {
      method: 'POST',
      credentials: 'include'
    }).then(r => r.json());
  },

  async getCurrentUser() {
    return fetch(`${API_BASE}/auth/me`, {
      credentials: 'include'
    }).then(r => r.json());
  }
};

// Task API
const TaskAPI = {
  async getTasks(filters = {}) {
    const params = new URLSearchParams(filters);
    return fetch(`${API_BASE}/tasks?${params}`, {
      credentials: 'include'
    }).then(r => r.json());
  },

  async createTask(taskData) {
    return fetch(`${API_BASE}/tasks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(taskData)
    }).then(r => r.json());
  },

  async updateTask(id, taskData) {
    return fetch(`${API_BASE}/tasks/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(taskData)
    }).then(r => r.json());
  },

  async deleteTask(id) {
    return fetch(`${API_BASE}/tasks/${id}`, {
      method: 'DELETE',
      credentials: 'include'
    }).then(r => r.json());
  },

  async archiveTask(id) {
    return fetch(`${API_BASE}/tasks/${id}/archive`, {
      method: 'POST',
      credentials: 'include'
    }).then(r => r.json());
  },

  async restoreTask(id) {
    return fetch(`${API_BASE}/tasks/${id}/restore`, {
      method: 'POST',
      credentials: 'include'
    }).then(r => r.json());
  }
};
```

### **Schritt 2: Firebase SDK entfernen**

**Änderungen in `public/index.html`:**

❌ Entfernen:
```html
<!-- Firebase imports -->
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-app.js"></script>
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-auth.js"></script>
<script src="https://www.gstatic.com/firebasejs/10.12.0/firebase-firestore.js"></script>

<!-- Firebase initialization -->
<script>
  const firebaseConfig = { ... };
  const app = initializeApp(firebaseConfig);
  const auth = getAuth(app);
  const db = getFirestore(app);
</script>
```

✅ Ersetzen mit:
```html
<script src="api-client.js"></script>
```

### **Schritt 3: Authentication neu implementieren**

**Alte Firebase Auth:**
```javascript
// onAuthStateChanged(auth, (user) => { ... })
// signInWithPopup(auth, provider)
// signInAnonymously(auth)
// signOut(auth)
```

**Neue REST API Auth:**
```javascript
async function handleLogin(email, password) {
  const result = await AuthAPI.login(email, password);
  if (result.success) {
    currentUser = result.data;
    localStorage.setItem('user', JSON.stringify(currentUser));
    renderBoard();
  } else {
    showError(result.error);
  }
}

async function handleLogout() {
  await AuthAPI.logout();
  currentUser = null;
  localStorage.removeItem('user');
  renderLoginForm();
}

async function initializeAuth() {
  try {
    const result = await AuthAPI.getCurrentUser();
    if (result.success && result.data) {
      currentUser = result.data;
      renderBoard();
    } else {
      renderLoginForm();
    }
  } catch (error) {
    renderLoginForm();
  }
}
```

### **Schritt 4: Daten-Zugriff migrieren**

**Alte Firestore Queries:**
```javascript
// getTasks
const q = query(collection(db, 'tasks'), where('userId', '==', user.uid));
const snap = await getDocs(q);

// Neue Task erstellen
await addDoc(collection(db, 'tasks'), taskData);

// Task aktualisieren
await updateDoc(doc(db, 'tasks', taskId), updates);

// Task löschen
await deleteDoc(doc(db, 'tasks', taskId));
```

**Neue REST API Calls:**
```javascript
// getTasks
const response = await TaskAPI.getTasks({ 
  columnName: filterColumn 
});
const tasks = response.data || [];

// Neue Task erstellen
await TaskAPI.createTask({
  title: form.title.value,
  description: form.description.value,
  taskType: form.taskType.value,
  owner: form.owner.value,
  customer: form.customer.value,
  columnName: 'Todo',
  tags: taskTags
});

// Task aktualisieren
await TaskAPI.updateTask(taskId, updatedData);

// Task löschen
await TaskAPI.deleteTask(taskId);

// Task archivieren
await TaskAPI.archiveTask(taskId);

// Task wiederherstellen
await TaskAPI.restoreTask(taskId);
```

### **Schritt 5: Storage & Caching anpassen**

**localStorage bleibt gleich für:**
- Theme Preference (light/dark)
- Temporäre UI State

**Entfernen:**
- Firestore IndexedDB Persistence
- Firebase Session Persistence

**Neu hinzufügen:**
- API Error Handling & Retry Logic
- Offline Queue (optional, da online-only)

### **Schritt 6: Testing & Validierung**

1. **Login/Logout Test**
   - Registrierung funktioniert
   - Login speichert httpOnly Cookie
   - Logout löscht Cookie

2. **Task CRUD Test**
   - Tasks laden
   - Task erstellen
   - Task aktualisieren
   - Task löschen

3. **Filter & Search**
   - Owner Filter
   - Customer Filter
   - Task Type Filter
   - Tags Filter

4. **Archive/Restore**
   - Task archivieren
   - Archivierte Tasks filtern
   - Task wiederherstellen

---

## Arbeitsbereiche

### **A. In HTML-Datei ändern:**
1. Firebase imports entfernen
2. `api-client.js` hinzufügen
3. Auth Functions umschreiben
4. Task CRUD Functions umschreiben
5. Event Handler aktualisieren
6. Error Handling anpassen

### **B. Neue Dateien erstellen:**
1. `public/api-client.js` - REST API Client
2. `PHASE4_IMPLEMENTATION.md` - Detaillierte Änderungen

### **C. Testing:**
1. Manuell alle Features testen
2. Browser DevTools prüfen (Cookies)
3. Backend Logs prüfen (Requests)
4. Error Handling testen

---

## Zeitschätzung

| Task | Zeit |
|------|------|
| REST API Client | 30 min |
| Firebase Code entfernen | 30 min |
| Auth Flows neu implementieren | 45 min |
| Task CRUD migrieren | 60 min |
| Error Handling & UI | 30 min |
| Testing & Bugs beheben | 45 min |
| **TOTAL** | **~3-4 Stunden** |

---

## Risiken & Mitigationen

| Risiko | Mitigation |
|--------|-----------|
| httpOnly Cookies CORS-Probleme | Backend CORS für localhost konfigurieren |
| Authentifizierung verliert State | Auf Cookie-Basis umstellen (persistent) |
| Offline Funktionalität | Online-only Design → kein Problem |
| Data Format Mismatch | API Response Struktur mit DB Schema vergleichen |
| Performance Regression | Lazy Loading & Pagination implementieren |

---

## Phase 4 Start: Sofort losleggen! 🚀
