# PrKanban Backend - Phase 3 Implementation

**Status**: ✅ Project Structure Ready  
**Framework**: ASP.NET Core 10  
**Database**: SQLite (D:\Apps\prKanban\Data\prkanban.db)  
**Authentication**: ASP.NET Core Identity  

---

## 📋 PROJECT STRUCTURE

```
PrKanban.Backend/
├── Controllers/
│   ├── AuthController.cs           ✅ Register, login, logout, user info
│   └── TasksController.cs          ✅ CRUD operations for tasks
├── Data/
│   └── PrKanbanContext.cs          ✅ EF Core DbContext
├── Models/
│   ├── ApplicationUser.cs          ✅ Extended IdentityUser
│   ├── Task.cs                     ✅ Task entity
│   └── BoardConfig.cs              ✅ Board configuration entity
├── Program.cs                      ✅ Startup configuration
├── appsettings.json                ✅ Database connection string
├── appsettings.Development.json    ✅ Development logging
└── PrKanban.Backend.csproj         ✅ Project file with dependencies
```

---

## 🚀 NEXT STEPS TO COMPLETE PHASE 3

### 1. Install .NET 10 SDK
```bash
# Download from https://dotnet.microsoft.com/en-us/download/dotnet/10.0
# Or use winget if available
winget install Microsoft.DotNet.SDK.10
```

### 2. Restore Dependencies
```bash
cd Backend/PrKanban.Backend
dotnet restore
```

### 3. Create Initial EF Core Migration
Use `dotnet-ef` version 10 to match the project's EF Core version.
```bash
dotnet ef migrations add InitialCreate
```

### 4. Create SQLite Database
```bash
# Ensure directory exists
mkdir -p D:\Apps\prKanban\Data

# Apply migrations to create database
dotnet ef database update
```

### 5. Run the Application
```bash
dotnet run
```

The API will be available at: `http://localhost:5000`

WIP limits can be configured independently for each Doing and Done subcolumn by
clicking its count. A limit warns when exceeded but does not block moves. The
current backend has no board-config endpoint, so these settings persist only in
the browser's local storage.

The four board columns can be collapsed independently using their side tabs.
Their open/closed state is shared across all visible swimlanes and is retained
while the page remains open. Dragging a task over a permitted collapsed column
opens it so a Doing or Done subcolumn can be selected. The Backlog and board
panels sit side by side; on narrow screens, scroll the board horizontally.
Use **Öffnen** or **Einklappen** in the board controls to open or collapse all
five panels without changing the swimlane grouping.
Swimlanes appear inside the board panels and stay aligned across columns.
Opening or closing a group in any panel applies to all panels; group counts
show the number of tasks in that panel.

The collapsible Backlog on the left holds tasks outside the workflow board.
Create tasks with its `+ Task` button, edit them by selecting a card, and move
them between the Backlog and permitted board columns by drag and drop or the
card's move menu. The task editor keeps Close and Save visible while its
contents scroll. Backlog tasks are stored through the same task API as board tasks.

---

## 📚 IMPLEMENTED ENDPOINTS

### Authentication (AuthController)
- ✅ `POST /api/v1/auth/register` - Create new user
- ✅ `POST /api/v1/auth/login` - Authenticate user
- ✅ `POST /api/v1/auth/logout` - End session
- ✅ `GET /api/v1/auth/user` - Get current user info

### Tasks (TasksController)
- ✅ `GET /api/v1/tasks` - List all tasks visible to Admin and User roles (with filtering)
- ✅ `GET /api/v1/tasks/:id` - Get any task visible to Admin and User roles
- ✅ `POST /api/v1/tasks` - Create task
- ✅ `PUT /api/v1/tasks/:id` - Update task
- ✅ `PUT /api/v1/tasks/:id/position` - Move a task and atomically reorder the destination lane
- ✅ `DELETE /api/v1/tasks/:id` - Delete task
- ✅ `PUT /api/v1/tasks/:id/archive` - Archive task
- ✅ `PUT /api/v1/tasks/:id/restore` - Restore archived task

Admin and User accounts can view, edit, move, archive, restore and delete any
task. "Assigned to" identifies the person responsible for a task; it does not
control permissions. Other roles, including a future Customer role, do not have
task API access unless explicitly authorized.

### Task attachments

- `GET /api/v1/tasks/:id/attachments` - List attachment metadata
- `GET /api/v1/tasks/:id/attachments/:attachmentId` - Get attachment metadata
- `POST /api/v1/tasks/:id/attachments` - Upload one file (up to 20 MB)
- `GET /api/v1/tasks/:id/attachments/:attachmentId/content` - Download a file or view a validated inline raster image
- `DELETE /api/v1/tasks/:id/attachments/:attachmentId` - Delete an attachment

Attachment contents are stored as SQLite BLOBs. Admins and Users can read,
upload and delete attachments on any task. Task descriptions are sanitized on the server and support
basic formatting, lists, links, and PNG/JPEG/GIF/WebP inline images.
Select an image and drag a resize handle to change its width while preserving
its aspect ratio. The size is saved with the task description.

### Administration
- `GET /api/v1/admin/database` - Read the database provider and location
- `GET /api/v1/admin/users` and `GET /api/v1/admin/users/:id` - List or retrieve accounts
- `POST /api/v1/admin/users` - Create an account with the User or Admin role
- `PUT /api/v1/admin/users/:id` - Update account details, role and active status

These endpoints require an authenticated, active administrator. Deactivation is
stored as an Identity claim; it prevents sign-in and invalidates existing sessions
on their next request. It does not delete the account or its tasks. Administrators
cannot deactivate themselves or remove their own administrator role.

Administrators can create and edit customers, projects and deliverables in
the **Customers and projects** tab next to the Kanban filters, also accessible
from administration. The Kanban tab preserves the current board. The Dashboard
contains Kanban KPIs and Budget KPIs tabs. Kanban KPIs uses the current customer,
project, assignee, type, tag and archive filters for current WIP and flow history.
Master-board column and subcolumn changes are recorded automatically for new and
existing tasks. Existing tasks receive a baseline at migration time, not an
invented historical entry date. Cycle time and lead time are averages of recorded
completions in the last 30 UTC calendar days; throughput counts those completions.
Cycle time requires a known entry into In Progress since the previous completion;
lead time uses task creation. Each subsequent completion of a reopened task counts
again. The CFD shows known daily column states for the same period; control and
scatter charts use known cycle times, with the scatter chart showing the nearest
rank 85th percentile. Aging shows time in the current master column for active
tasks with known entry times. History follows current task attributes and filters,
not historical assignments; deleted tasks and their history are removed.
Select an individual control-chart or scatterplot point, or an Aging WIP bar,
to open its task on the Kanban board with the current filters preserved. When
scatterplot points overlap exactly, select the desired task from the offered list.
Budget KPIs is a placeholder for future metrics.
Users can browse the hierarchy and, like Admins, reorder items and move them
between valid parents. Only Admins can create items or edit their fields.
The expandable tree shows tasks under their deliverables, including archived
tasks marked as such. Tasks without a deliverable are not shown in the tree.
Admins and Users can drag customers, projects, deliverables and tasks to reorder
them or move them to another valid parent; the hierarchy order is independent
from Kanban card order.
Every project belongs to one customer;
every deliverable belongs to one project. Customer debtor numbers and CRM URLs,
project CRM links, order numbers and default assignees are optional.
Deliverable estimates, budgets and actuals are non-negative hours. Actuals are
entered manually until time recording is implemented; exceeding the budget is allowed.
Each project has a unique, immutable 2–12 character alphanumeric short key,
beginning with a letter. New deliverable and task IDs start with that key.
IDs remain stable when items are reassigned; existing IDs are not changed.
Existing projects need a short key before new deliverables or tasks can be created.
The project's default assignee pre-fills new tasks and can be overridden.
Admins and Users can read the hierarchy and must select a deliverable for new
tasks. Existing unassigned tasks remain available and can be assigned when
edited. Budgets do not restrict task creation; time booking is not implemented yet.

Each project has its own board. Administrators and assigned project leads can
use **Manage columns** to create, rename, reorder (by dragging the handle or
using the arrow buttons) and delete columns, map each
column to a master-board category, and optionally display Doing/Done subcolumns.
Backlog mappings do not support subcolumns. Columns containing tasks cannot be
remapped or deleted, and each master category must retain at least one mapping.

- `GET /api/v1/hierarchy` — hierarchy for Admin and User
- `POST /api/v1/hierarchy/customers`, `/projects`, `/deliverables` — create (Admin)
- `PUT /api/v1/hierarchy/{customers|projects|deliverables}/:id` — edit (Admin)
- `PUT /api/v1/hierarchy/{customers|projects|deliverables|tasks}/:id/position` — move/reorder (Admin and User)

### Languages and translations

The board language selector is immediately before the theme button. German is
the default; signed-in users' choices are saved with their accounts, while a
signed-out choice lasts for the current page. Administrators can edit German and
English texts in Administration > Translations. Reload the app to pick up saved
changes; deleted built-in keys fall back to their original texts. Text templates
may contain placeholders such as `{count}` which should be retained.

- `GET /api/v1/translations` - Read UI texts and built-in fallbacks
- `PUT /api/v1/auth/language` - Save `{"language":"de"}` or `{"language":"en"}` for the signed-in user
- `GET /api/v1/admin/translations` - List translations (administrator only)
- `GET`, `POST`, `PUT`, `DELETE /api/v1/admin/translations/:key` - Manage a translation (administrator only)

Task positions within columns are saved per task. Existing tasks without a saved
position retain their creation-time ordering until they are reordered.

---

## 🔐 SECURITY FEATURES CONFIGURED

✅ **ASP.NET Core Identity** - User management with password hashing  
✅ **Cookie Authentication** - Session-based auth (httpOnly, Secure, SameSite)  
✅ **Authorization** - [Authorize] attribute on protected endpoints  
✅ **Password Policy** - 8 char min, digit required  
✅ **Account Lockout** - 5 failed attempts = 15 min lockout  
✅ **HTTPS** - Enforced in production  

---

## 🗄️ DATABASE

**Location**: `D:\Apps\prKanban\Data\prkanban.db`

**Tables Created by EF Core**:
- `AspNetUsers` - User accounts (Identity)
- `AspNetRoles` - User roles
- `AspNetUserRoles` - User role assignments
- `Tasks` - Kanban tasks
- `BoardConfigs` - Board layout configurations

**Connection String**: 
```
Data Source=D:\Apps\prKanban\Data\prkanban.db;Mode=ReadWrite;Cache=Shared;Journal Mode=Wal;Busy Timeout=5000;
```

---

## 📖 TESTING THE API

### 1. Register a New User
```bash
curl -X POST http://localhost:5000/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecurePass123",
    "confirmPassword": "SecurePass123",
    "fullName": "Test User"
  }'
```

### 2. Login
```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -c cookies.txt \
  -d '{
    "email": "test@example.com",
    "password": "SecurePass123"
  }'
```

### 3. Create a Task
```bash
curl -X POST http://localhost:5000/api/v1/tasks \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d '{
    "title": "Build login form",
    "owner": "John Doe",
    "columnName": "todo",
    "tags": ["frontend", "urgent"]
  }'
```

### 4. Get All Tasks
```bash
curl -X GET "http://localhost:5000/api/v1/tasks?column=todo" \
  -H "Content-Type: application/json" \
  -b cookies.txt
```

---

## 🧪 UNIT TESTS (TODO - Phase 3+)

Test file structure to be added:

```
Tests/
├── AuthControllerTests.cs
├── TasksControllerTests.cs
└── PrKanbanContextTests.cs
```

Run tests:
```bash
dotnet test
```

---

## 📊 SWAGGER DOCUMENTATION

When running in development, Swagger UI is available at:
```
http://localhost:5000/swagger
```

---

## 🔧 CONFIGURATION

### appsettings.json
- Database connection string
- Logging levels
- Authentication policies
- Session timeouts

### Modify as needed:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=D:\\Apps\\prKanban\\Data\\prkanban.db;..."
  },
  "Authentication": {
    "PasswordPolicy": {
      "RequiredLength": 8,
      "RequireDigit": true
    },
    "Lockout": {
      "MaxFailedAccessAttempts": 5,
      "LockoutDurationMinutes": 15
    }
  }
}
```

---

## 📝 CODE STRUCTURE

### Controllers
- Handle HTTP requests
- Implement authorization checks
- Return standardized responses

### Models
- `ApplicationUser` - Extended IdentityUser
- `Task` - Kanban task
- `BoardConfig` - Board configuration

### Data
- `PrKanbanContext` - EF Core DbContext
- Manages database interactions
- Automatic timestamp updates

---

## ⚠️ IMPORTANT NOTES

1. **Database Path**: Ensure `D:\Apps\prKanban\Data\` directory exists before running
2. **Connection String**: Update appsettings.json if using different database location
3. **HTTPS**: Development uses HTTP, production requires HTTPS
4. **Secrets**: Never commit appsettings with secrets - use User Secrets or environment variables

---

## 🎯 SUCCESS CRITERIA FOR PHASE 3

- [ ] .NET 10 SDK installed
- [ ] Dependencies restored (`dotnet restore`)
- [ ] EF Core migration created (`dotnet ef migrations add InitialCreate`)
- [ ] Database created (`dotnet ef database update`)
- [ ] Application runs (`dotnet run`)
- [ ] Swagger UI accessible at `http://localhost:5000/swagger`
- [ ] Register endpoint works
- [ ] Login endpoint works
- [ ] Task CRUD endpoints work
- [ ] Authorization is enforced
- [ ] Tests pass (`dotnet test`)

---

## 📞 DOCUMENTATION REFERENCES

For detailed architecture and design, refer to:
- `PHASE2_ARCHITECTURE.md` - System architecture
- `PHASE2_DATABASE.md` - Database schema and models
- `PHASE2_API_SPECIFICATION.md` - API endpoints
- `PHASE2_AUTHENTICATION.md` - Authentication implementation

---

**Backend Status**: ✅ PROJECT SKELETON READY FOR PHASE 3 IMPLEMENTATION
