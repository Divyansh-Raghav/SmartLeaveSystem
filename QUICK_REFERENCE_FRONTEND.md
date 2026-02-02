# Quick Reference - Angular Frontend Implementation Checklist

## 📋 Copy-Paste Ready Prompts for Visual Studio Code

---

## ✅ QUICK SETUP REFERENCE

### Backend API Endpoints
```
POST   http://localhost:5000/api/auth/register
POST   http://localhost:5000/api/auth/login
GET    http://localhost:5000/api/leaverequests (requires token)
```

### Request/Response Examples

#### Register Request
```json
{
  "username": "john_emp",
  "email": "john@company.com",
  "password": "SecurePassword123!",
  "role": "Employee"
}
```

#### Login Request
```json
{
  "email": "john@company.com",
  "password": "SecurePassword123!"
}
```

#### Auth Response (Success)
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "john_emp",
    "email": "john@company.com",
    "role": "Employee"
  }
}
```

---

## 🎯 16-STEP IMPLEMENTATION CHECKLIST

### ✓ PHASE 1: Create Models & Services

**Step 1:** Create file `src/app/core/models/auth.models.ts`
- [ ] Copy-paste: All interfaces (LoginRequest, RegisterRequest, AuthResponse, UserDto, AuthState)
- [ ] Location: Core folder → models subfolder

**Step 2:** Create file `src/app/core/services/auth.service.ts`
- [ ] Copy-paste: AuthService class with login, register, logout methods
- [ ] Features: localStorage management, Observable streams, token handling
- [ ] Location: Core folder → services subfolder

**Step 3:** Create file `src/app/core/interceptors/auth.interceptor.ts`
- [ ] Copy-paste: AuthInterceptor class
- [ ] Features: Add Bearer token to requests, handle 401 errors
- [ ] Location: Core folder → interceptors subfolder

**Step 4:** Create file `src/app/core/guards/auth.guard.ts`
- [ ] Copy-paste: AuthGuard class
- [ ] Features: Protect routes, check roles
- [ ] Location: Core folder → guards subfolder

---

### ✓ PHASE 2: Create Login Component

**Step 5:** Create file `src/app/features/auth/login/login.component.ts`
- [ ] Copy-paste: LoginComponent class (standalone: true)
- [ ] Features: Form validation, error handling, redirect on success
- [ ] Location: Features → auth → login

**Step 6:** Create file `src/app/features/auth/login/login.component.html`
- [ ] Copy-paste: Login form template with Bootstrap styling
- [ ] Features: Email/password fields, error messages, register link
- [ ] Location: Same as step 5

**Step 7:** Create file `src/app/features/auth/login/login.component.css`
- [ ] Copy-paste: Styling for card, form, buttons
- [ ] Location: Same as step 5

---

### ✓ PHASE 3: Create Register Component

**Step 8:** Create file `src/app/features/auth/register/register.component.ts`
- [ ] Copy-paste: RegisterComponent class (standalone: true)
- [ ] Features: Form validation, password matching, role selection
- [ ] Location: Features → auth → register

**Step 9:** Create file `src/app/features/auth/register/register.component.html`
- [ ] Copy-paste: Register form template with Bootstrap styling
- [ ] Features: Username, email, password, confirm password, role dropdown, login link
- [ ] Location: Same as step 8

**Step 10:** Create file `src/app/features/auth/register/register.component.css`
- [ ] Copy-paste: Styling for card, form, buttons
- [ ] Location: Same as step 8

---

### ✓ PHASE 4: Configure Application

**Step 11:** Update `src/main.ts` OR `src/app/app.module.ts`
- [ ] Add HTTP_INTERCEPTORS provider for AuthInterceptor
- [ ] Add provideHttpClient() if using standalone
- [ ] Location: Root configuration file

**Step 12:** Update `src/environments/environment.ts`
- [ ] Add apiUrl property: `"http://localhost:5000"`
- [ ] Location: Environments folder

**Step 13:** Update `src/environments/environment.prod.ts`
- [ ] Add apiUrl property with production domain
- [ ] Location: Environments folder

**Step 14:** Update `src/app/app.routes.ts` or `app-routing.module.ts`
- [ ] Add /auth/login and /auth/register routes
- [ ] Add AuthGuard to protected routes
- [ ] Add role-based routing (optional: /leave-approvals for Manager)
- [ ] Location: App root routing file

---

### ✓ PHASE 5: Update App Component

**Step 15:** Update `src/app/app.component.ts`
- [ ] Inject AuthService
- [ ] Add currentUser$ and isAuthenticated$ observables
- [ ] Add logout() method
- [ ] Add isManager() helper method
- [ ] Location: App root component

**Step 16:** Update `src/app/app.component.html`
- [ ] Create navbar with Bootstrap styling
- [ ] Show login/register links if NOT authenticated
- [ ] Show navigation & user dropdown if authenticated
- [ ] Add logout functionality
- [ ] Add Manager-only menu items
- [ ] Location: App root template

---

## 🔐 JWT Token Flow Diagram

```
┌─────────────────┐
│  User Registers │
└────────┬────────┘
         │
         ▼
┌──────────────────────────────────┐
│ POST /api/auth/register          │
│ Body: username, email, password  │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ Backend validates & hashes pwd   │
│ Creates JWT token                │
│ Returns token + user info        │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ Frontend stores in localStorage: │
│ - authToken                      │
│ - currentUser (JSON)             │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ Next API calls include:          │
│ Header: Authorization: Bearer X  │
└────────┬─────────────────────────┘
         │
         ▼
┌──────────────────────────────────┐
│ AuthInterceptor attaches token   │
│ to every HTTP request            │
└──────────────────────────────────┘
```

---

## 🧪 Testing Checklist

After implementing all 16 steps:

- [ ] **Test Register**: Go to `/auth/register`, create new user
- [ ] **Test Login**: Go to `/auth/login`, login with credentials
- [ ] **Test Token Storage**: Check localStorage for authToken & currentUser
- [ ] **Test Protected Route**: Try accessing protected route without token (should redirect to login)
- [ ] **Test Authorization Header**: Open Network tab, verify Authorization header on API calls
- [ ] **Test Role-Based Access**: Login as Employee and Manager, check UI differences
- [ ] **Test Logout**: Logout should clear localStorage and redirect to login
- [ ] **Test Token Expiry**: (After 2 hours) Token should be invalid, redirect to login

---

## 🔧 Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| "Cannot find module" errors | Check file paths match exactly (case-sensitive on Linux/Mac) |
| API calls fail with CORS | Ensure backend allows CORS for `http://localhost:4200` |
| Token not sent in requests | Verify AuthInterceptor is registered in app config |
| Route guards not working | Ensure AuthGuard is imported in routes configuration |
| localStorage empty after refresh | Check browser's localStorage is not disabled |
| Login form not submitting | Verify ReactiveFormsModule is imported in component |
| Navigation navbar not showing | Check RouterLink is imported in app.component imports |

---

## 📦 Required Angular Modules/Imports Summary

```typescript
// In components
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive } from '@angular/router';

// In services
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';

// In app config
import { provideHttpClient } from '@angular/common/http';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { provideRouter } from '@angular/router';
```

---

## 🚀 Backend Integration Points

Your Angular app will call these backend endpoints:

```typescript
// In AuthService
POST /api/auth/register
POST /api/auth/login

// In LeaveRequestsService (add Authorization header automatically)
GET    /api/leaverequests
GET    /api/leaverequests/{id}
GET    /api/leaverequests/employee/{employeeId}
POST   /api/leaverequests
PUT    /api/leaverequests/{id}
DELETE /api/leaverequests/{id}
```

All protected endpoints require:
```
Header: Authorization: Bearer <jwt_token>
```

---

## 📱 Component Tree

```
AppComponent (navbar with auth)
├── LoginComponent (route: /auth/login)
├── RegisterComponent (route: /auth/register)
├── LeaveRequestsComponent (protected, requires [AuthGuard])
├── EmployeesComponent (protected, requires [AuthGuard])
└── LeaveApprovalsComponent (protected, requires [AuthGuard] + Manager role)
```

---

## 🎓 Key Concepts Implemented

✅ **Reactive Forms** - Email, password validation with real-time feedback
✅ **RxJS Observables** - State management with BehaviorSubject
✅ **HTTP Interceptor** - Automatic token injection, error handling
✅ **Route Guards** - Protect routes with AuthGuard
✅ **JWT Token** - Store in localStorage, validate on each request
✅ **Role-Based Access** - Different UI for Employee vs Manager
✅ **Error Handling** - Display validation errors, API errors
✅ **Standalone Components** - Modern Angular 14+ syntax

