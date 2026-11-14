# Admin User Setup Guide

## ✅ Admin User Created Successfully!

An admin user has been created for the prKanban application.

## Login Credentials

**Email:** `admin@prkanban.local`  
**Password:** Use the password configured for the account.

## Access URLs

- **Frontend:** http://localhost:3000
- **Backend:** http://localhost:5000

## How to Login

1. Open http://localhost:3000 in your browser
2. Click the **Login** button
3. When prompted, enter:
   - Email: `admin@prkanban.local`
   - Password: The password configured for the account
4. Click **Login**
5. You should now see the Kanban board

## Testing Checklist

After logging in, test the following features:

- [ ] **Create Task** - Add a new task to the board
- [ ] **Move Tasks** - Drag tasks between columns
- [ ] **Edit Task** - Click on a task to view/edit details
- [ ] **Delete Task** - Remove a task from the board
- [ ] **Persistence** - Refresh the page; tasks should still be there
- [ ] **Guest Login** - Test guest login without credentials
- [ ] **Logout** - Logout and try to access protected areas

## Backend Status

✅ Backend is running on http://localhost:5000  
✅ Database is initialized with SQLite  
✅ Authentication is working with httpOnly cookies  
✅ All API endpoints are operational

## API Endpoints

### Authentication

- `POST /api/v1/auth/register` - Register new user
- `POST /api/v1/auth/login` - Login user
- `POST /api/v1/auth/logout` - Logout user
- `GET /api/v1/auth/user` - Get current user info

### Tasks

- `GET /api/v1/tasks` - List all tasks
- `POST /api/v1/tasks` - Create new task
- `GET /api/v1/tasks/{id}` - Get task details
- `PUT /api/v1/tasks/{id}` - Update task
- `DELETE /api/v1/tasks/{id}` - Delete task

## Troubleshooting

### Login not working
- Ensure backend is running on port 5000
- Check that database has been initialized
- Clear browser cookies and try again

### Tasks not persisting
- Verify backend is running
- Check browser console for errors
- Ensure httpOnly cookies are being sent

### Backend issues
- Restart the backend: `cd Backend/PrKanban.Backend && dotnet run`
- Check database file: `prkanban.db` in project root
- Review backend logs for errors

## Security Notes

- Passwords are hashed using ASP.NET Core Identity (PBKDF2)
- Sessions use httpOnly cookies (cannot be accessed via JavaScript)
- SameSite=Strict cookie policy prevents CSRF attacks
- User data is stored in SQLite database with encryption

## Next Steps

After successful testing:

1. **Phase 6.1:** Browser Compatibility Testing
2. **Phase 6.2:** Login Modal Implementation (replace prompt)
3. **Phase 6.3:** Error Handling Improvements
4. **Phase 6.4:** Performance Optimization
5. **Phase 6.5:** Security Audit

See `PHASE6_ROADMAP.md` for detailed roadmap.

---

**Created:** $(date)  
**Status:** ✅ Ready for Testing
