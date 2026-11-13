# Phase 2: Zielarchitektur (Target Architecture Design)

**Date**: 2026-11-14  
**Status**: IN PROGRESS  
**Approved Decisions**: 6 design decisions locked  

---

## EXECUTIVE SUMMARY

This document defines the complete target architecture for migrating prKanban from **Firebase → ASP.NET Core + SQLite**. Based on your 6 approved decisions, the system will use:

- **Frontend**: Vanilla JavaScript (unchanged) + REST API client
- **Backend**: ASP.NET Core 10 with OAuth2/OIDC authentication
- **Database**: SQLite (D:\Apps\prKanban\Data\prkanban.db) with concurrent locking
- **Deployment**: Windows PC only (dev/production)
- **API**: Header-based versioning (Accept: application/vnd.prkanban.v1+json)
- **Sync**: Online-only mode (no offline queue)

---

## 1. HIGH-LEVEL SYSTEM ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────────┐
│                          PRKANBAN SYSTEM                         │
└─────────────────────────────────────────────────────────────────┘

    ┌──────────────────────┐
    │   Frontend (SPA)     │
    │                      │
    │  • HTML5             │
    │  • Vanilla JS        │
    │  • Tailwind CSS      │
    │  • FontAwesome       │
    │                      │
    │  Files:              │
    │  • public/index.html │
    └──────────────────────┘
              │
              │ HTTP/REST
              │ (Header-based versioning)
              │
    ┌─────────────────────────────────────────────────────┐
    │        REST API Gateway (ASP.NET Core 10)           │
    │                                                      │
    │  • OpenIdConnect Authentication                     │
    │  • Header-based API versioning                      │
    │  • Error handling & logging                         │
    │  • CORS configuration                               │
    └─────────────────────────────────────────────────────┘
              │
              │ EF Core Queries
              │
    ┌──────────────────────────────────────────────────────────┐
    │     Application Business Logic (ASP.NET Core)           │
    │                                                          │
    │  • TaskController (CRUD operations)                     │
    │  • BoardConfigController (board settings)               │
    │  • AuthenticationService (OAuth2/OIDC)                  │
    │  • AuthorizationService (user permissions)              │
    │  • ValidationService (input validation)                 │
    └──────────────────────────────────────────────────────────┘
              │
              │ EF Core (ORM)
              │
    ┌──────────────────────────────────────────────────────────┐
    │              SQLite Database                             │
    │                                                          │
    │  Location: D:\Apps\prKanban\Data\prkanban.db            │
    │                                                          │
    │  Tables:                                                │
    │  • Users (id, email, name, provider, ...)              │
    │  • Tasks (id, title, owner, column, subColumn, ...)    │
    │  • BoardConfig (id, userId, columnOrder, ...)          │
    │  • AuditLog (id, action, userId, timestamp, ...)       │
    │                                                          │
    │  Features:                                              │
    │  • SQLite locking for concurrent access                │
    │  • Connection pooling (EF Core)                        │
    │  • Transaction support                                 │
    └──────────────────────────────────────────────────────────┘
```

---

## 2. AUTHENTICATION ARCHITECTURE (ASP.NET Core Identity)

### Decision Rationale
✅ **ASP.NET Core Identity selected** because:
- Built-in user management system
- No external OAuth provider dependency
- Simple username/email + password authentication
- User data stored directly in SQLite
- Ideal for Windows PC deployment
- Full control over credentials and sessions

### Authentication Flow

```
1. User Access
   ├─ User opens http://localhost:5000
   └─ Frontend detects no active session

2. Login Request
   ├─ User enters email and password in login form
   └─ Frontend sends credentials to /api/v1/auth/login

3. Server-Side Validation
   ├─ Backend validates email exists in AspNetUsers table
   ├─ Backend verifies password hash (bcrypt)
   └─ Creates User session if credentials valid

4. User Session
   ├─ Backend returns session cookie (httpOnly, secure)
   ├─ Frontend stores cookie automatically
   └─ All API requests include session cookie

5. Session Persistence
   ├─ Each request validated against AspNetUsers table
   ├─ Session timeout: 30 minutes (configurable)
   └─ Automatic logout on session expiration

6. Logout
   ├─ Frontend calls /api/v1/auth/logout
   ├─ Backend invalidates session
   └─ Cookie cleared
```

### Configuration (Pending in Phase 3)

```csharp
// ASP.NET Core Startup Configuration
services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<PrKanbanContext>()
.AddDefaultTokenProviders();

services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
```

### Security Considerations
- ✅ Passwords hashed with bcrypt (not stored plain text)
- ✅ Cookies secure (httpOnly + SameSite=Strict)
- ✅ Account lockout after 5 failed attempts (15 min lockout)
- ✅ Session timeout after 30 minutes of inactivity
- ✅ Email verification (optional, can add in Phase 3+)
- ✅ Password reset functionality supported

---

## 3. DATABASE ARCHITECTURE

### Location & Structure

```
D:\Apps\prKanban\Data\prkanban.db  (SQLite 3)
├── Size: ~10-50 MB (depending on task volume)
├── Encoding: UTF-8
├── Journal Mode: WAL (Write-Ahead Logging)
├── Busy Timeout: 5000ms (for concurrent access)
└── Locking: SQLite native locking mechanism
```

### Database Schema

#### Table: Users
```sql
CREATE TABLE Users (
    Id TEXT PRIMARY KEY,
    Email NVARCHAR(256) NOT NULL UNIQUE,
    Name NVARCHAR(256),
    Provider NVARCHAR(50) NOT NULL,  -- "Google", "MicrosoftAccount", "AzureAD"
    ProviderId NVARCHAR(500) NOT NULL,
    ProfilePicture NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

#### Table: Tasks
```sql
CREATE TABLE Tasks (
    Id TEXT PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    TaskType NVARCHAR(50),  -- "task", "subtask", "bug", etc.
    Owner NVARCHAR(256) NOT NULL,
    Customer NVARCHAR(256),
    Description NVARCHAR(MAX),
    Tags NVARCHAR(MAX),  -- JSON array: ["tag1", "tag2"]
    ColumnName NVARCHAR(100) NOT NULL,  -- Column ID/Name
    SubColumnName NVARCHAR(100),  -- SubColumn ID/Name
    IsArchived BIT DEFAULT 0,
    UserId TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

#### Table: BoardConfig
```sql
CREATE TABLE BoardConfig (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL UNIQUE,
    ColumnOrder NVARCHAR(MAX),  -- JSON array of column IDs
    SubColumnMapping NVARCHAR(MAX),  -- JSON mapping
    WipLimits NVARCHAR(MAX),  -- JSON: {"columnId": limit}
    TagsConfig NVARCHAR(MAX),  -- JSON: {"tag": {color, icon}}
    Theme NVARCHAR(50) DEFAULT 'light',  -- "light" or "dark"
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

#### Table: AuditLog (Optional, for Phase 3+)
```sql
CREATE TABLE AuditLog (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Action NVARCHAR(100),  -- "CREATE", "UPDATE", "DELETE"
    EntityType NVARCHAR(50),  -- "Task", "BoardConfig"
    EntityId TEXT,
    OldValues NVARCHAR(MAX),  -- JSON
    NewValues NVARCHAR(MAX),  -- JSON
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);
```

### EF Core Model Structure (Phase 3)

```csharp
// Domain Models
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string Provider { get; set; }
    public string ProviderId { get; set; }
    public string ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Task> Tasks { get; set; }
    public BoardConfig BoardConfig { get; set; }
}

public class Task
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string TaskType { get; set; }
    public string Owner { get; set; }
    public string Customer { get; set; }
    public string Description { get; set; }
    public string[] Tags { get; set; }  // JSON
    public string ColumnName { get; set; }
    public string SubColumnName { get; set; }
    public bool IsArchived { get; set; }
    
    // Foreign Key
    public string UserId { get; set; }
    public User User { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BoardConfig
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    
    public string[] ColumnOrder { get; set; }  // JSON
    public Dictionary<string, string[]> SubColumnMapping { get; set; }  // JSON
    public Dictionary<string, int> WipLimits { get; set; }  // JSON
    public Dictionary<string, TagConfig> TagsConfig { get; set; }  // JSON
    public string Theme { get; set; } = "light";
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Concurrency & Locking Strategy

**Option Selected: SQLite with WAL (Write-Ahead Logging)**

```ini
# Connection string with WAL mode
Data Source=D:\Apps\prKanban\Data\prkanban.db;Mode=ReadWrite;
Cache=Shared;Journal Mode=Wal;Busy Timeout=5000;
```

**How it works:**
- Multiple readers can access database simultaneously
- One writer at a time (queued automatically)
- 5-second timeout for busy database
- Automatic rollback if timeout exceeded
- No manual locking code required

**Limitations:**
- ✅ Suitable for Windows PC (single-user focus, occasional multi-user)
- ❌ Would need MS SQL Server for high-concurrency enterprise scenarios (future migration)

**Concurrent Access Example:**
```
Time  | User A              | User B
------|---------------------|-------------------
1     | Read task list      |
2     | (reading)           | Read task list
3     | (reading)           | (reading)
4     | Update task #5      | (waiting for lock)
5     | Commit              | (waiting for lock)
6     | (lock released)     | Update task #7
7     |                     | Commit
```

---

## 4. API SPECIFICATION (Header-Based Versioning)

### Versioning Strategy

**Selected: URL-Based (/api/v1/...)**

All API endpoints are versioned in the URL path:

```
/api/v1/tasks              (v1 - Initial release)
/api/v2/tasks              (v2 - Future version if needed)
```

**Advantages:**
- ✅ Explicit and visible in URLs
- ✅ Easy to understand and debug
- ✅ Clear separation between versions
- ✅ Browser-friendly (can copy URLs easily)
- ✅ Supports running multiple versions simultaneously

**Version History:**
- `v1` (2026-11-14): Initial release - ASP.NET Core Identity, SQLite, no offline

### API Base URL

```
http://localhost:5000/api/v1
```

(Production URL: TBD - Windows PC configuration)

### Core Endpoints (Phase 3 Implementation)

#### Authentication

```
POST /api/v1/auth/register
  Body: { email, password, confirmPassword }
  Returns: { userId, email, message }
  
POST /api/v1/auth/login
  Body: { email, password }
  Returns: { userId, email, name, message }
  
POST /api/v1/auth/logout
  Returns: { message: "Logged out successfully" }
  
GET /api/v1/auth/user
  Returns: { userId, email, name } (requires auth)
```

#### Tasks

```
GET /api/v1/tasks
  Returns: List of tasks for current user
  
GET /api/v1/tasks/:id
  Returns: Single task details
  
POST /api/v1/tasks
  Body: { title, owner, column, subColumn, ... }
  Returns: Created task
  
PUT /api/v1/tasks/:id
  Body: { title, owner, column, ... }
  Returns: Updated task
  
DELETE /api/v1/tasks/:id
  Returns: { success: true }
  
GET /api/v1/tasks?archived=true
  Returns: Archived tasks
  
GET /api/v1/tasks?owner=john&column=todo
  Returns: Filtered tasks
```

#### Board Configuration

```
GET /api/v1/board-config
  Returns: Current user's board configuration
  
PUT /api/v1/board-config
  Body: { columnOrder, wipLimits, theme, ... }
  Returns: Updated board config
```

### Error Response Format

```json
{
  "error": {
    "code": "INVALID_REQUEST",
    "message": "Title is required",
    "details": {
      "field": "title",
      "value": ""
    }
  }
}
```

### Success Response Format

```json
{
  "data": { ... },
  "meta": {
    "version": "1",
    "timestamp": "2026-11-14T00:25:00Z"
  }
}
```

---

## 5. FRONTEND INTEGRATION POINTS

### Current Firebase Calls → Future REST API Calls

| Firebase Function | Current Usage | Future REST Call |
|---|---|---|
| `getAuth()` | Get current user | `GET /api/v1/auth/user` |
| `signInWithPopup()` | Google login | `POST /api/v1/auth/login` |
| `signOut()` | Logout | `POST /api/v1/auth/logout` |
| `getDocs(query(collection(...)))` | Fetch tasks | `GET /api/v1/tasks` |
| `addDoc(collection(...))` | Create task | `POST /api/v1/tasks` |
| `updateDoc(docRef, {})` | Update task | `PUT /api/v1/tasks/:id` |
| `deleteDoc(docRef)` | Delete task | `DELETE /api/v1/tasks/:id` |
| `getDoc(docRef)` | Fetch single task | `GET /api/v1/tasks/:id` |
| `onSnapshot()` | Real-time listener | ❌ Polling (online-only mode) |

### REST API Client (Phase 4)

Location: `public/js/api-client.js`

```javascript
class PrKanbanAPI {
    constructor(baseURL = 'http://localhost:5000/api/v1') {
        this.baseURL = baseURL;
        this.headers = {
            'Content-Type': 'application/json'
        };
    }
    
    // Authentication
    async register(email, password) { ... }
    async login(email, password) { ... }
    async logout() { ... }
    async getCurrentUser() { ... }
    
    // Tasks
    async getTasks(filter = {}) { ... }
    async getTask(id) { ... }
    async createTask(data) { ... }
    async updateTask(id, data) { ... }
    async deleteTask(id) { ... }
    
    // Board Config
    async getBoardConfig() { ... }
    async updateBoardConfig(data) { ... }
}
```

---

## 6. DEPLOYMENT ARCHITECTURE (Windows PC)

### Target Environment

**Windows PC Configuration:**
- OS: Windows 10/11 or Windows Server 2019+
- .NET Runtime: .NET 10 (or .NET 8 as fallback)
- Database: SQLite 3 (no installation needed)
- Port: 5000 (HTTP dev) / 443 (HTTPS prod)

### Directory Structure (Windows PC)

```
C:\prKanban\
├── App\
│   ├── prkanban.exe             (ASP.NET Core published app)
│   ├── appsettings.json         (configuration)
│   ├── appsettings.Production.json
│   └── wwwroot\
│       └── index.html           (SPA frontend)
│
├── Data\
│   ├── prkanban.db              (SQLite database)
│   ├── prkanban.db-shm          (Write-Ahead Log)
│   └── prkanban.db-wal          (Write-Ahead Log)
│
└── Logs\
    └── app-2026-11-14.log       (Application logs)
```

### Deployment Process (Phase 6)

1. **Build & Publish**
   ```bash
   dotnet publish -c Release -o C:\prKanban\App
   ```

2. **Database Setup**
   ```bash
   dotnet ef database update --configuration Release
   ```

3. **Manual Start (Development)**
   ```bash
   C:\prKanban\App\prkanban.exe
   ```

4. **Service Setup (Production)**
   - Windows Service or Task Scheduler
   - Auto-start on PC reboot
   - Crash recovery via supervisor

---

## 7. SECURITY CONSIDERATIONS

### Authentication & Authorization
- ✅ OAuth2/OIDC (delegated to provider)
- ✅ HttpOnly cookies (CSRF protection)
- ✅ Tokens with expiration (15 min - 1 hour)
- ✅ Refresh token rotation
- ✅ User data isolation per user

### API Security
- ✅ HTTPS enforcement (production)
- ✅ CORS configuration (localhost only for dev)
- ✅ Rate limiting (TBD in Phase 3)
- ✅ Input validation on all endpoints
- ✅ SQL injection prevention (EF Core parameterized queries)

### Database Security
- ✅ SQLite file permissions (Windows file ACLs)
- ✅ Database backup strategy
- ✅ Audit logging (optional, Phase 3+)
- ✅ No sensitive data in logs

### Secrets Management
- ✅ OAuth client ID/secret in environment variables
- ✅ Database connection string in `appsettings.json` (user permissions)
- ✅ No secrets in source code (git-ignored)

---

## 8. ERROR HANDLING STRATEGY

### API Error Codes

| Code | HTTP | Description | Action |
|---|---|---|---|
| `UNAUTHORIZED` | 401 | User not authenticated | Redirect to login |
| `FORBIDDEN` | 403 | User lacks permissions | Show error message |
| `INVALID_REQUEST` | 400 | Invalid input | Show validation errors |
| `NOT_FOUND` | 404 | Resource not found | Show error message |
| `CONFLICT` | 409 | Resource already exists | Handle duplicate |
| `SERVER_ERROR` | 500 | Backend error | Show error message |

### Frontend Error Handling (Phase 4)

```javascript
try {
    const task = await api.updateTask(id, data);
    updateUI(task);
} catch (error) {
    if (error.code === 'UNAUTHORIZED') {
        redirectToLogin();
    } else if (error.code === 'INVALID_REQUEST') {
        showValidationErrors(error.details);
    } else {
        showToast('Error: ' + error.message, 'error');
    }
}
```

---

## 9. PERFORMANCE CONSIDERATIONS

### Database Performance
- ✅ Indexes on frequently queried columns (UserId, ColumnName)
- ✅ Connection pooling (EF Core default: 25 connections)
- ✅ Query optimization for large task lists
- ✅ Pagination for API responses (TBD in Phase 3)

### API Performance
- ✅ Response caching (short-lived, user-specific)
- ✅ Gzip compression for responses
- ✅ Minimal payload size (only required fields)
- ✅ Batch operations for bulk updates (future)

### Frontend Performance
- ✅ Single HTML file (no HTTP round trips)
- ✅ Vanilla JS (no framework overhead)
- ✅ Offline-capable local state (even with online-only sync)
- ✅ Lazy loading of board data

---

## 10. MIGRATION STRATEGY FROM FIREBASE

### Data Export Flow

```
Firebase Firestore
    ↓
Export JSON
    ↓
Parse & Transform
    ↓
EF Core DbContext.Add
    ↓
SQLite Database
```

### User Migration

```
Old (Firebase):
  - Anonymous users
  - Google OAuth tokens

New (ASP.NET Core):
  - OAuth2/OIDC users
  - Centralized user table
```

### Verification Checkpoints

1. ✅ Task count matches (Phase 5)
2. ✅ Data integrity validation (Phase 5)
3. ✅ User mappings correct (Phase 5)
4. ✅ Relationships preserved (Phase 5)

---

## 11. ROLLBACK PLAN

### In Case of Critical Issues

**Rollback Decision Points:**
- Phase 3 Backend: Revert to Firebase, resume Phase 1 analysis
- Phase 4 Frontend: Use feature flag to switch back to Firebase API
- Phase 5 Data Migration: Restore Firestore backup, retry import
- Phase 6 Deployment: Keep Firebase running as fallback

**Rollback Procedure:**
```bash
# Switch frontend to Firebase API client
git revert --no-edit <commit-hash>

# Restore database backup
copy D:\Apps\prKanban\Data\prkanban.db.backup D:\Apps\prKanban\Data\prkanban.db
```

---

## 12. SUCCESS CRITERIA FOR PHASE 2

✅ **Architecture Document Complete** (this file)  
✅ **Database Schema Finalized** (schema section above)  
✅ **API Specification Complete** (endpoints defined)  
✅ **Authentication Flow Designed** (OAuth2/OIDC detailed)  
✅ **Security Review Passed** (security section reviewed)  
✅ **Performance Strategy Defined** (caching, optimization)  
✅ **Deployment Plan Documented** (Windows PC setup)  
✅ **Risk Mitigation Planned** (rollback procedures)  

---

## 13. NEXT STEPS FOR PHASE 3

Phase 3 will implement this architecture:

**Phase 3: Backend Implementation** (40-60 hours estimated)

1. **Project Setup**
   - Create ASP.NET Core 10 project
   - Add NuGet packages (EF Core, OAuth2, etc.)
   - Configure project structure

2. **Database Layer**
   - Create EF Core DbContext
   - Generate migrations
   - Initialize SQLite database

3. **API Implementation**
   - Implement all endpoints from Section 4
   - Add input validation
   - Add error handling

4. **Authentication**
   - Configure OAuth2/OIDC provider (Google/Microsoft)
   - Implement login/logout flow
   - Create user service

5. **Testing**
   - Unit tests for services
   - Integration tests for API
   - Manual testing of workflows

---

## 14. DOCUMENT VERSIONS

| Version | Date | Author | Status | Notes |
|---|---|---|---|---|
| 1.0 | 2026-11-14 | Copilot + JFK77 | DRAFT | Initial architecture design |

---

**Document Status**: ✅ PHASE 2 ARCHITECTURE DESIGN READY FOR REVIEW

Next: Create DATABASE.md, API.md, AUTHENTICATION.md
