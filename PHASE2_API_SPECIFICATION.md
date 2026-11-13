# Phase 2: REST API Specification (URL-Based Versioning v1)

**Date**: 2026-11-14  
**Status**: COMPLETE  
**Version**: 1.0  
**Base URL**: `http://localhost:5000/api/v1`  

---

## EXECUTIVE SUMMARY

This document specifies the complete REST API for prKanban, including:
- ✅ Authentication endpoints (login, logout, register)
- ✅ Task management endpoints (CRUD operations)
- ✅ Board configuration endpoints
- ✅ Error handling standards
- ✅ Response formats
- ✅ Request/response examples

---

## 1. API OVERVIEW

### Base URL
```
Development: http://localhost:5000/api/v1
Production:  http://<windows-pc-ip>:5000/api/v1
```

### Versioning Strategy
**URL-Based**: Version included in path `/api/v1/`

### Authentication
- **Type**: Session-based (httpOnly cookies)
- **Method**: ASP.NET Core Identity
- **Credentials**: Email + Password

### Response Format
All responses are JSON with consistent structure:

**Success Response**:
```json
{
  "success": true,
  "data": { ... },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Error Response**:
```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": { ... }
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

---

## 2. AUTHENTICATION ENDPOINTS

### 2.1 Register User

**Endpoint**: `POST /api/v1/auth/register`

**Description**: Create a new user account

**Request Body**:
```json
{
  "email": "john@example.com",
  "password": "SecurePassword123!",
  "confirmPassword": "SecurePassword123!"
}
```

**Validation Rules**:
- Email: Valid email format, unique in system
- Password: Minimum 8 characters, must contain:
  - At least one uppercase letter
  - At least one number
  - At least one special character (!@#$%^&*)

**Success Response** (HTTP 201 Created):
```json
{
  "success": true,
  "data": {
    "userId": "user_550e8400-e29b-41d4-a716-446655440000",
    "email": "john@example.com",
    "message": "User registered successfully"
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `EMAIL_ALREADY_EXISTS` | 409 | Email already registered |
| `INVALID_EMAIL` | 400 | Email format invalid |
| `WEAK_PASSWORD` | 400 | Password doesn't meet requirements |
| `PASSWORD_MISMATCH` | 400 | Passwords don't match |

**Example Error** (HTTP 409):
```json
{
  "success": false,
  "error": {
    "code": "EMAIL_ALREADY_EXISTS",
    "message": "An account with this email already exists",
    "details": {
      "email": "john@example.com"
    }
  }
}
```

---

### 2.2 Login User

**Endpoint**: `POST /api/v1/auth/login`

**Description**: Authenticate user and create session

**Request Body**:
```json
{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "userId": "user_550e8400-e29b-41d4-a716-446655440000",
    "email": "john@example.com",
    "name": "John Doe",
    "message": "Logged in successfully"
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Headers** (Set by server):
```
Set-Cookie: .AspNetCore.Identity.Application=<encrypted-session>; 
  Path=/; 
  HttpOnly; 
  Secure; 
  SameSite=Strict; 
  Max-Age=1800
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `INVALID_CREDENTIALS` | 401 | Email/password incorrect |
| `ACCOUNT_LOCKED` | 403 | Too many failed attempts (5 attempts = 15min lockout) |
| `ACCOUNT_DISABLED` | 403 | User account disabled (admin action) |

**Example Error** (HTTP 401):
```json
{
  "success": false,
  "error": {
    "code": "INVALID_CREDENTIALS",
    "message": "Email or password is incorrect",
    "details": null
  }
}
```

---

### 2.3 Logout User

**Endpoint**: `POST /api/v1/auth/logout`

**Description**: End user session and clear authentication cookie

**Request Body**: None

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "message": "Logged out successfully"
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Headers** (Set by server):
```
Set-Cookie: .AspNetCore.Identity.Application=; 
  Path=/; 
  Expires=Thu, 01 Jan 1970 00:00:00 GMT
```

**Requirements**:
- ✅ User must be authenticated (cookie present)
- ✅ Works even if session already expired

---

### 2.4 Get Current User

**Endpoint**: `GET /api/v1/auth/user`

**Description**: Get authenticated user's profile information

**Request Body**: None

**Requirements**:
- ✅ User must be authenticated (valid session cookie)

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "userId": "user_550e8400-e29b-41d4-a716-446655440000",
    "email": "john@example.com",
    "name": "John Doe",
    "createdAt": "2026-11-10T14:30:00Z",
    "lastLoginAt": "2026-11-14T00:30:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `UNAUTHORIZED` | 401 | Not authenticated / session expired |

**Example Error** (HTTP 401):
```json
{
  "success": false,
  "error": {
    "code": "UNAUTHORIZED",
    "message": "You must be logged in to access this resource",
    "details": null
  }
}
```

---

## 3. TASK ENDPOINTS

### 3.1 Get All Tasks

**Endpoint**: `GET /api/v1/tasks`

**Description**: Retrieve all tasks for authenticated user with optional filtering

**Query Parameters**:
| Parameter | Type | Required | Description |
|---|---|---|---|
| `archived` | boolean | No | Filter by archived status (true/false) |
| `column` | string | No | Filter by column name |
| `owner` | string | No | Filter by task owner |
| `customer` | string | No | Filter by customer |
| `tag` | string | No | Filter by tag (supports multiple: ?tag=urgent&tag=frontend) |
| `skip` | integer | No | Pagination: number of items to skip (default: 0) |
| `take` | integer | No | Pagination: number of items to return (default: 100, max: 500) |

**Requirements**:
- ✅ User must be authenticated

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "tasks": [
      {
        "id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "title": "Build login form",
        "taskType": "task",
        "owner": "John Doe",
        "customer": "Acme Corp",
        "description": "Create login/register UI with validation",
        "tags": ["frontend", "urgent"],
        "columnName": "in-progress",
        "subColumnName": "ready",
        "isArchived": false,
        "createdAt": "2026-11-13T14:45:30Z",
        "updatedAt": "2026-11-14T00:30:00Z"
      },
      {
        "id": "task_b2c3d4e5-f6a7-8901-bcde-f12345678901",
        "title": "Write API tests",
        "taskType": "task",
        "owner": "Jane Smith",
        "customer": "Beta Inc",
        "description": "Unit tests for all endpoints",
        "tags": ["backend", "testing"],
        "columnName": "todo",
        "subColumnName": null,
        "isArchived": false,
        "createdAt": "2026-11-14T00:15:00Z",
        "updatedAt": "2026-11-14T00:15:00Z"
      }
    ],
    "pagination": {
      "total": 47,
      "returned": 2,
      "skip": 0,
      "take": 100
    }
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Example Requests**:
```bash
# Get all tasks
GET /api/v1/tasks

# Get active (non-archived) tasks in "in-progress" column
GET /api/v1/tasks?archived=false&column=in-progress

# Get tasks owned by John with "urgent" tag
GET /api/v1/tasks?owner=John&tag=urgent

# Pagination: skip first 50, return next 25
GET /api/v1/tasks?skip=50&take=25

# Multiple filters combined
GET /api/v1/tasks?customer=Acme&tag=urgent&tag=frontend&archived=false
```

---

### 3.2 Get Single Task

**Endpoint**: `GET /api/v1/tasks/:id`

**Description**: Retrieve details for a single task

**URL Parameters**:
- `id` (required): Task ID (format: "task_<uuid>")

**Requirements**:
- ✅ User must be authenticated
- ✅ User must own the task (or be admin)

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "Build login form",
    "taskType": "task",
    "owner": "John Doe",
    "customer": "Acme Corp",
    "description": "Create login/register UI with validation",
    "tags": ["frontend", "urgent"],
    "columnName": "in-progress",
    "subColumnName": "ready",
    "isArchived": false,
    "createdAt": "2026-11-13T14:45:30Z",
    "updatedAt": "2026-11-14T00:30:00Z",
    "userId": "user_550e8400-e29b-41d4-a716-446655440000"
  },
  "meta": {
    "timestamp": "2026-11-14T00:35:00Z",
    "version": "1"
  }
}
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `NOT_FOUND` | 404 | Task doesn't exist |
| `FORBIDDEN` | 403 | User doesn't have access to this task |
| `UNAUTHORIZED` | 401 | Not authenticated |

---

### 3.3 Create Task

**Endpoint**: `POST /api/v1/tasks`

**Description**: Create a new task

**Request Body**:
```json
{
  "title": "Build login form",
  "taskType": "task",
  "owner": "John Doe",
  "customer": "Acme Corp",
  "description": "Create login/register UI with validation",
  "tags": ["frontend", "urgent"],
  "columnName": "todo",
  "subColumnName": null
}
```

**Validation Rules**:
- `title`: Required, max 255 characters
- `taskType`: Optional, defaults to "task"
- `owner`: Optional, max 256 characters
- `customer`: Optional, max 256 characters
- `description`: Optional, no limit
- `tags`: Optional, array of strings
- `columnName`: Required, must exist in user's board config
- `subColumnName`: Optional, if provided must exist in column mapping

**Requirements**:
- ✅ User must be authenticated

**Success Response** (HTTP 201 Created):
```json
{
  "success": true,
  "data": {
    "id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "Build login form",
    "taskType": "task",
    "owner": "John Doe",
    "customer": "Acme Corp",
    "description": "Create login/register UI with validation",
    "tags": ["frontend", "urgent"],
    "columnName": "todo",
    "subColumnName": null,
    "isArchived": false,
    "createdAt": "2026-11-14T00:40:00Z",
    "updatedAt": "2026-11-14T00:40:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T00:40:00Z",
    "version": "1"
  }
}
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `INVALID_REQUEST` | 400 | Required field missing or invalid |
| `INVALID_COLUMN` | 400 | Column name not found in board config |
| `UNAUTHORIZED` | 401 | Not authenticated |

---

### 3.4 Update Task

**Endpoint**: `PUT /api/v1/tasks/:id`

**Description**: Update an existing task

**URL Parameters**:
- `id` (required): Task ID

**Request Body** (any/all fields optional):
```json
{
  "title": "Build login and register forms",
  "owner": "Jane Smith",
  "columnName": "in-progress",
  "subColumnName": "review",
  "tags": ["frontend", "urgent", "high-priority"]
}
```

**Requirements**:
- ✅ User must be authenticated
- ✅ User must own the task

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "Build login and register forms",
    "taskType": "task",
    "owner": "Jane Smith",
    "customer": "Acme Corp",
    "description": "Create login/register UI with validation",
    "tags": ["frontend", "urgent", "high-priority"],
    "columnName": "in-progress",
    "subColumnName": "review",
    "isArchived": false,
    "updatedAt": "2026-11-14T00:45:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T00:45:00Z",
    "version": "1"
  }
}
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `NOT_FOUND` | 404 | Task doesn't exist |
| `FORBIDDEN` | 403 | User doesn't have permission to update |
| `INVALID_COLUMN` | 400 | Column name invalid |
| `UNAUTHORIZED` | 401 | Not authenticated |

---

### 3.5 Delete Task

**Endpoint**: `DELETE /api/v1/tasks/:id`

**Description**: Delete (hard delete) a task

**URL Parameters**:
- `id` (required): Task ID

**Requirements**:
- ✅ User must be authenticated
- ✅ User must own the task

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "message": "Task deleted successfully",
    "taskId": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890"
  },
  "meta": {
    "timestamp": "2026-11-14T00:50:00Z",
    "version": "1"
  }
}
```

**Error Responses**:

| Code | HTTP | Reason |
|---|---|---|
| `NOT_FOUND` | 404 | Task doesn't exist |
| `FORBIDDEN` | 403 | User doesn't have permission to delete |
| `UNAUTHORIZED` | 401 | Not authenticated |

---

### 3.6 Archive Task (Soft Delete)

**Endpoint**: `PUT /api/v1/tasks/:id/archive`

**Description**: Archive a task (soft delete, not permanent)

**URL Parameters**:
- `id` (required): Task ID

**Request Body**: None

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "Build login form",
    "isArchived": true,
    "updatedAt": "2026-11-14T00:55:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T00:55:00Z",
    "version": "1"
  }
}
```

---

### 3.7 Restore Archived Task

**Endpoint**: `PUT /api/v1/tasks/:id/restore`

**Description**: Restore an archived task

**URL Parameters**:
- `id` (required): Task ID

**Request Body**: None

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "id": "task_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "title": "Build login form",
    "isArchived": false,
    "updatedAt": "2026-11-14T01:00:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T01:00:00Z",
    "version": "1"
  }
}
```

---

## 4. BOARD CONFIGURATION ENDPOINTS

### 4.1 Get Board Configuration

**Endpoint**: `GET /api/v1/board-config`

**Description**: Get the authenticated user's board layout configuration

**Requirements**:
- ✅ User must be authenticated

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "id": "config_550e8400-e29b-41d4-a716-446655440000",
    "userId": "user_550e8400-e29b-41d4-a716-446655440000",
    "columnOrder": [
      "backlog",
      "todo",
      "in-progress",
      "review",
      "done"
    ],
    "subColumnMapping": {
      "backlog": ["blocked", "ready"],
      "in-progress": ["ready", "blocked"],
      "review": ["waiting-feedback"]
    },
    "wipLimits": {
      "in-progress": 5,
      "review": 3
    },
    "tagsConfig": {
      "urgent": {
        "color": "#FF0000",
        "icon": "exclamation-circle"
      },
      "frontend": {
        "color": "#0066FF",
        "icon": "code"
      },
      "bug": {
        "color": "#FF6600",
        "icon": "bug"
      }
    },
    "theme": "dark",
    "createdAt": "2026-11-10T10:00:00Z",
    "updatedAt": "2026-11-14T00:30:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T01:05:00Z",
    "version": "1"
  }
}
```

---

### 4.2 Update Board Configuration

**Endpoint**: `PUT /api/v1/board-config`

**Description**: Update the authenticated user's board configuration

**Request Body** (any/all fields optional):
```json
{
  "columnOrder": [
    "backlog",
    "todo",
    "in-progress",
    "review",
    "testing",
    "done"
  ],
  "subColumnMapping": {
    "backlog": ["blocked", "ready"],
    "in-progress": ["ready", "blocked"],
    "review": ["waiting-feedback"],
    "testing": ["manual", "automated"]
  },
  "wipLimits": {
    "in-progress": 6,
    "review": 3,
    "testing": 4
  },
  "theme": "light"
}
```

**Requirements**:
- ✅ User must be authenticated
- ✅ If `columnOrder` provided, must include valid column names

**Success Response** (HTTP 200 OK):
```json
{
  "success": true,
  "data": {
    "id": "config_550e8400-e29b-41d4-a716-446655440000",
    "columnOrder": ["backlog", "todo", "in-progress", "review", "testing", "done"],
    "wipLimits": {
      "in-progress": 6,
      "review": 3,
      "testing": 4
    },
    "theme": "light",
    "updatedAt": "2026-11-14T01:10:00Z"
  },
  "meta": {
    "timestamp": "2026-11-14T01:10:00Z",
    "version": "1"
  }
}
```

---

## 5. ERROR HANDLING

### Error Code Reference

| Code | HTTP | Description | Client Action |
|---|---|---|---|
| `UNAUTHORIZED` | 401 | User not authenticated | Redirect to login |
| `FORBIDDEN` | 403 | User lacks permission | Show error message |
| `NOT_FOUND` | 404 | Resource not found | Show error message |
| `INVALID_REQUEST` | 400 | Invalid input data | Show validation errors |
| `CONFLICT` | 409 | Resource already exists | Handle duplicate |
| `VALIDATION_ERROR` | 400 | Field validation failed | Show field-level errors |
| `ACCOUNT_LOCKED` | 403 | Account locked (too many login attempts) | Show lockout message |
| `SERVER_ERROR` | 500 | Unexpected server error | Show generic error |

### Detailed Error Response Format

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred",
    "details": {
      "title": ["Title is required"],
      "columnName": ["Column name must be one of: backlog, todo, in-progress, review, done"],
      "email": ["Email format is invalid"]
    }
  },
  "meta": {
    "timestamp": "2026-11-14T01:15:00Z",
    "version": "1"
  }
}
```

---

## 6. RATE LIMITING (Future Enhancement)

**Planned for Phase 3+**:
- Max 100 requests per minute per user
- Max 5 failed login attempts per 15 minutes
- Response Header: `X-RateLimit-Remaining: 95`

---

## 7. CORS CONFIGURATION

### Development (localhost)

```csharp
services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### Production

```csharp
services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("http://<windows-pc-ip>:5000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

---

## 8. AUTHENTICATION FLOW (Client Implementation)

### Login Flow Example

```javascript
async function login(email, password) {
    try {
        const response = await fetch('http://localhost:5000/api/v1/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',  // Include cookies
            body: JSON.stringify({ email, password })
        });
        
        const result = await response.json();
        
        if (result.success) {
            // Store user info (not password!)
            sessionStorage.setItem('currentUser', JSON.stringify(result.data));
            
            // Redirect to dashboard
            window.location.href = '/dashboard';
        } else {
            // Show error
            showError(result.error.message);
        }
    } catch (error) {
        showError('Network error: ' + error.message);
    }
}
```

### API Request with Authentication

```javascript
async function getTasks() {
    try {
        const response = await fetch('http://localhost:5000/api/v1/tasks', {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            },
            credentials: 'include'  // Automatically sends session cookie
        });
        
        const result = await response.json();
        
        if (response.status === 401) {
            // Session expired, redirect to login
            window.location.href = '/login';
        } else if (result.success) {
            return result.data.tasks;
        } else {
            showError(result.error.message);
        }
    } catch (error) {
        showError('Network error: ' + error.message);
    }
}
```

---

## 9. API CLIENT LIBRARY (Phase 4)

Reference implementation in `public/js/api-client.js`:

```javascript
class PrKanbanAPI {
    constructor(baseURL = 'http://localhost:5000/api/v1') {
        this.baseURL = baseURL;
    }
    
    // Authentication
    async register(email, password, confirmPassword) {
        return this._request('POST', '/auth/register', {
            email, password, confirmPassword
        });
    }
    
    async login(email, password) {
        return this._request('POST', '/auth/login', {
            email, password
        });
    }
    
    async logout() {
        return this._request('POST', '/auth/logout');
    }
    
    async getCurrentUser() {
        return this._request('GET', '/auth/user');
    }
    
    // Tasks
    async getTasks(filters = {}) {
        const query = new URLSearchParams(filters).toString();
        return this._request('GET', `/tasks${query ? '?' + query : ''}`);
    }
    
    async getTask(id) {
        return this._request('GET', `/tasks/${id}`);
    }
    
    async createTask(data) {
        return this._request('POST', '/tasks', data);
    }
    
    async updateTask(id, data) {
        return this._request('PUT', `/tasks/${id}`, data);
    }
    
    async deleteTask(id) {
        return this._request('DELETE', `/tasks/${id}`);
    }
    
    // Board Config
    async getBoardConfig() {
        return this._request('GET', '/board-config');
    }
    
    async updateBoardConfig(data) {
        return this._request('PUT', '/board-config', data);
    }
    
    // Internal method
    async _request(method, path, body = null) {
        const options = {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include'
        };
        
        if (body) options.body = JSON.stringify(body);
        
        const response = await fetch(this.baseURL + path, options);
        const data = await response.json();
        
        if (!response.ok) {
            throw new APIError(data.error.code, data.error.message, data.error.details);
        }
        
        return data.data;
    }
}

class APIError extends Error {
    constructor(code, message, details) {
        super(message);
        this.code = code;
        this.details = details;
    }
}
```

---

## 10. SUCCESS CRITERIA FOR API SPECIFICATION

✅ **All endpoints defined** (auth, tasks, board config)  
✅ **Request/response formats documented** (examples provided)  
✅ **Error handling standardized** (error codes, messages)  
✅ **Authentication flow specified** (session-based)  
✅ **CORS configuration defined** (dev/production)  
✅ **Client usage examples** (JavaScript implementation)  
✅ **Rate limiting planned** (future enhancement)  

---

**Document Status**: ✅ API SPECIFICATION COMPLETE

Next: Create PHASE2_AUTHENTICATION.md (final Phase 2 document)
