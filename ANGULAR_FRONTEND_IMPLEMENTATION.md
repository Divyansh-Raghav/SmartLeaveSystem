# Angular Frontend Implementation - Authentication Phase
## Smart Leave Management System

This file contains exact prompts and file locations for implementing the Angular frontend for Login & Register functionality.

---

## 📋 PREREQUISITE
Your Angular project structure should look like:
```
src/
├── app/
│   ├── core/
│   │   ├── guards/
│   │   ├── interceptors/
│   │   └── services/
│   ├── shared/
│   ├── features/
│   │   └── auth/
│   │       ├── login/
│   │       ├── register/
│   │       └── auth.module.ts (or use standalone components)
│   ├── app.module.ts (or app.config.ts for standalone)
│   └── app.component.ts
├── environments/
│   └── environment.ts
└── ...
```

---

## 🔧 STEP 1: Create Models/Interfaces
**File Location:** `src/app/core/models/auth.models.ts`

**Create this file with these contents:**

```typescript
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  role: string; // "Employee" or "Manager"
}

export interface AuthResponse {
  success: boolean;
  message: string;
  token?: string;
  user?: UserDto;
}

export interface UserDto {
  id: number;
  username: string;
  email: string;
  role: string;
}

export interface AuthState {
  user: UserDto | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}
```

---

## 🔧 STEP 2: Create Auth Service
**File Location:** `src/app/core/services/auth.service.ts`

**Create this file with these contents:**

```typescript
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, catchError } from 'rxjs';
import { 
  LoginRequest, 
  RegisterRequest, 
  AuthResponse, 
  UserDto 
} from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/auth`;
  
  private currentUserSubject: BehaviorSubject<UserDto | null>;
  public currentUser$: Observable<UserDto | null>;
  
  private isAuthenticatedSubject: BehaviorSubject<boolean>;
  public isAuthenticated$: Observable<boolean>;

  constructor(private http: HttpClient) {
    // Initialize from localStorage
    const storedUser = localStorage.getItem('currentUser');
    const storedToken = localStorage.getItem('authToken');
    
    this.currentUserSubject = new BehaviorSubject<UserDto | null>(
      storedUser ? JSON.parse(storedUser) : null
    );
    this.currentUser$ = this.currentUserSubject.asObservable();
    
    this.isAuthenticatedSubject = new BehaviorSubject<boolean>(!!storedToken);
    this.isAuthenticated$ = this.isAuthenticatedSubject.asObservable();
  }

  // Getter for current user value
  public get currentUserValue(): UserDto | null {
    return this.currentUserSubject.value;
  }

  // Getter for authentication status
  public get isAuthenticatedValue(): boolean {
    return this.isAuthenticatedSubject.value;
  }

  // Getter for token
  public getToken(): string | null {
    return localStorage.getItem('authToken');
  }

  // Login method
  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(response => {
        if (response.success && response.token && response.user) {
          localStorage.setItem('authToken', response.token);
          localStorage.setItem('currentUser', JSON.stringify(response.user));
          this.currentUserSubject.next(response.user);
          this.isAuthenticatedSubject.next(true);
        }
      }),
      catchError(error => {
        throw error;
      })
    );
  }

  // Register method
  register(credentials: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, credentials).pipe(
      tap(response => {
        if (response.success && response.token && response.user) {
          localStorage.setItem('authToken', response.token);
          localStorage.setItem('currentUser', JSON.stringify(response.user));
          this.currentUserSubject.next(response.user);
          this.isAuthenticatedSubject.next(true);
        }
      }),
      catchError(error => {
        throw error;
      })
    );
  }

  // Logout method
  logout(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    this.currentUserSubject.next(null);
    this.isAuthenticatedSubject.next(false);
  }

  // Check if user has specific role
  hasRole(role: string): boolean {
    return this.currentUserValue?.role === role;
  }
}
```

---

## 🔧 STEP 3: Create Auth Interceptor
**File Location:** `src/app/core/interceptors/auth.interceptor.ts`

**Create this file with these contents:**

```typescript
import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    // Get token from AuthService
    const token = this.authService.getToken();

    // Add token to request if it exists
    if (token) {
      request = request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
    }

    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // Handle 401 Unauthorized - token expired or invalid
        if (error.status === 401) {
          this.authService.logout();
          this.router.navigate(['/auth/login']);
        }

        return throwError(() => error);
      })
    );
  }
}
```

---

## 🔧 STEP 4: Create Auth Guard
**File Location:** `src/app/core/guards/auth.guard.ts`

**Create this file with these contents:**

```typescript
import { Injectable } from '@angular/core';
import {
  Router,
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree
} from '@angular/router';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    
    if (this.authService.isAuthenticatedValue) {
      // Check if route requires specific role
      if (route.data['roles'] && route.data['roles'].length > 0) {
        const hasRole = route.data['roles'].includes(this.authService.currentUserValue?.role);
        if (!hasRole) {
          this.router.navigate(['/unauthorized']);
          return false;
        }
      }
      return true;
    }

    // Not logged in, redirect to login page
    this.router.navigate(['/auth/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }
}
```

---

## 🔧 STEP 5: Create Login Component
**File Location:** `src/app/features/auth/login/login.component.ts`

**Create this file with these contents:**

```typescript
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loading = false;
  submitted = false;
  error = '';
  returnUrl: string = '';

  constructor(
    private formBuilder: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.formBuilder.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });

    // Get return url from query params or default to '/'
    this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
  }

  get f() {
    return this.loginForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.error = '';

    if (this.loginForm.invalid) {
      return;
    }

    this.loading = true;

    this.authService.login(this.loginForm.value).subscribe({
      next: (response) => {
        if (response.success) {
          this.router.navigateByUrl(this.returnUrl);
        }
      },
      error: (error) => {
        this.error = error.error?.message || 'Login failed. Please check your credentials.';
        this.loading = false;
      }
    });
  }
}
```

---

## 🔧 STEP 6: Create Login Template
**File Location:** `src/app/features/auth/login/login.component.html`

**Create this file with these contents:**

```html
<div class="container mt-5">
  <div class="row justify-content-center">
    <div class="col-md-6">
      <div class="card">
        <div class="card-header bg-primary text-white">
          <h4 class="mb-0">Login</h4>
        </div>
        <div class="card-body">
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
            <!-- Error Message -->
            <div *ngIf="error" class="alert alert-danger alert-dismissible fade show" role="alert">
              {{ error }}
              <button type="button" class="btn-close" (click)="error = ''"></button>
            </div>

            <!-- Email Field -->
            <div class="mb-3">
              <label for="email" class="form-label">Email Address</label>
              <input
                type="email"
                class="form-control"
                id="email"
                formControlName="email"
                [class.is-invalid]="submitted && f['email'].errors"
              />
              <div class="invalid-feedback" *ngIf="submitted && f['email'].errors">
                <div *ngIf="f['email'].errors['required']">Email is required</div>
                <div *ngIf="f['email'].errors['email']">Email must be valid</div>
              </div>
            </div>

            <!-- Password Field -->
            <div class="mb-3">
              <label for="password" class="form-label">Password</label>
              <input
                type="password"
                class="form-control"
                id="password"
                formControlName="password"
                [class.is-invalid]="submitted && f['password'].errors"
              />
              <div class="invalid-feedback" *ngIf="submitted && f['password'].errors">
                <div *ngIf="f['password'].errors['required']">Password is required</div>
              </div>
            </div>

            <!-- Submit Button -->
            <button
              type="submit"
              class="btn btn-primary w-100"
              [disabled]="loading"
            >
              <span *ngIf="loading" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              {{ loading ? 'Logging in...' : 'Login' }}
            </button>
          </form>

          <!-- Register Link -->
          <div class="text-center mt-3">
            <p class="text-muted">
              Don't have an account? 
              <a routerLink="/auth/register" class="text-primary">Register here</a>
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
```

---

## 🔧 STEP 7: Create Login Styles
**File Location:** `src/app/features/auth/login/login.component.css`

**Create this file with these contents:**

```css
.card {
  box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
  border: none;
  border-radius: 8px;
}

.card-header {
  border-radius: 8px 8px 0 0;
  font-weight: 600;
}

.form-control:focus {
  border-color: #0d6efd;
  box-shadow: 0 0 0 0.2rem rgba(13, 110, 253, 0.25);
}

.btn-primary {
  background-color: #0d6efd;
  border-color: #0d6efd;
  font-weight: 500;
  padding: 0.5rem;
}

.btn-primary:hover {
  background-color: #0b5ed7;
  border-color: #0a58ca;
}

.text-muted {
  font-size: 0.9rem;
}

a {
  text-decoration: none;
}

a:hover {
  text-decoration: underline;
}
```

---

## 🔧 STEP 8: Create Register Component
**File Location:** `src/app/features/auth/register/register.component.ts`

**Create this file with these contents:**

```typescript
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  loading = false;
  submitted = false;
  error = '';
  success = '';

  constructor(
    private formBuilder: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.registerForm = this.formBuilder.group({
      username: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required],
      role: ['Employee', Validators.required]
    }, {
      validators: this.passwordMatchValidator
    });
  }

  // Custom validator for password match
  passwordMatchValidator(form: FormGroup): { [key: string]: boolean } | null {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (password && confirmPassword && password.value !== confirmPassword.value) {
      return { 'passwordMismatch': true };
    }
    return null;
  }

  get f() {
    return this.registerForm.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.error = '';
    this.success = '';

    if (this.registerForm.invalid) {
      return;
    }

    this.loading = true;

    const { confirmPassword, ...registerData } = this.registerForm.value;

    this.authService.register(registerData).subscribe({
      next: (response) => {
        if (response.success) {
          this.success = 'Registration successful! Redirecting to dashboard...';
          setTimeout(() => {
            this.router.navigate(['/']);
          }, 2000);
        }
      },
      error: (error) => {
        this.error = error.error?.message || 'Registration failed. Please try again.';
        this.loading = false;
      }
    });
  }
}
```

---

## 🔧 STEP 9: Create Register Template
**File Location:** `src/app/features/auth/register/register.component.html`

**Create this file with these contents:**

```html
<div class="container mt-5">
  <div class="row justify-content-center">
    <div class="col-md-6">
      <div class="card">
        <div class="card-header bg-success text-white">
          <h4 class="mb-0">Create Account</h4>
        </div>
        <div class="card-body">
          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
            <!-- Error Message -->
            <div *ngIf="error" class="alert alert-danger alert-dismissible fade show" role="alert">
              {{ error }}
              <button type="button" class="btn-close" (click)="error = ''"></button>
            </div>

            <!-- Success Message -->
            <div *ngIf="success" class="alert alert-success alert-dismissible fade show" role="alert">
              {{ success }}
            </div>

            <!-- Username Field -->
            <div class="mb-3">
              <label for="username" class="form-label">Username</label>
              <input
                type="text"
                class="form-control"
                id="username"
                formControlName="username"
                [class.is-invalid]="submitted && f['username'].errors"
              />
              <div class="invalid-feedback" *ngIf="submitted && f['username'].errors">
                Username is required
              </div>
            </div>

            <!-- Email Field -->
            <div class="mb-3">
              <label for="email" class="form-label">Email Address</label>
              <input
                type="email"
                class="form-control"
                id="email"
                formControlName="email"
                [class.is-invalid]="submitted && f['email'].errors"
              />
              <div class="invalid-feedback" *ngIf="submitted && f['email'].errors">
                <div *ngIf="f['email'].errors['required']">Email is required</div>
                <div *ngIf="f['email'].errors['email']">Email must be valid</div>
              </div>
            </div>

            <!-- Password Field -->
            <div class="mb-3">
              <label for="password" class="form-label">Password</label>
              <input
                type="password"
                class="form-control"
                id="password"
                formControlName="password"
                [class.is-invalid]="submitted && f['password'].errors"
              />
              <div class="invalid-feedback" *ngIf="submitted && f['password'].errors">
                <div *ngIf="f['password'].errors['required']">Password is required</div>
                <div *ngIf="f['password'].errors['minlength']">Password must be at least 6 characters</div>
              </div>
            </div>

            <!-- Confirm Password Field -->
            <div class="mb-3">
              <label for="confirmPassword" class="form-label">Confirm Password</label>
              <input
                type="password"
                class="form-control"
                id="confirmPassword"
                formControlName="confirmPassword"
                [class.is-invalid]="submitted && (f['confirmPassword'].errors || registerForm.errors?.['passwordMismatch'])"
              />
              <div class="invalid-feedback" *ngIf="submitted && f['confirmPassword'].errors">
                Confirm Password is required
              </div>
              <div class="invalid-feedback" *ngIf="submitted && registerForm.errors?.['passwordMismatch']">
                Passwords do not match
              </div>
            </div>

            <!-- Role Selection -->
            <div class="mb-3">
              <label for="role" class="form-label">Role</label>
              <select
                class="form-select"
                id="role"
                formControlName="role"
              >
                <option value="Employee">Employee</option>
                <option value="Manager">Manager</option>
              </select>
            </div>

            <!-- Submit Button -->
            <button
              type="submit"
              class="btn btn-success w-100"
              [disabled]="loading"
            >
              <span *ngIf="loading" class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
              {{ loading ? 'Creating Account...' : 'Register' }}
            </button>
          </form>

          <!-- Login Link -->
          <div class="text-center mt-3">
            <p class="text-muted">
              Already have an account? 
              <a routerLink="/auth/login" class="text-success">Login here</a>
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</div>
```

---

## 🔧 STEP 10: Create Register Styles
**File Location:** `src/app/features/auth/register/register.component.css`

**Create this file with these contents:**

```css
.card {
  box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
  border: none;
  border-radius: 8px;
}

.card-header {
  border-radius: 8px 8px 0 0;
  font-weight: 600;
}

.form-control:focus,
.form-select:focus {
  border-color: #198754;
  box-shadow: 0 0 0 0.2rem rgba(25, 135, 84, 0.25);
}

.btn-success {
  background-color: #198754;
  border-color: #198754;
  font-weight: 500;
  padding: 0.5rem;
}

.btn-success:hover {
  background-color: #157347;
  border-color: #146c43;
}

.text-muted {
  font-size: 0.9rem;
}

a {
  text-decoration: none;
}

a:hover {
  text-decoration: underline;
}
```

---

## 🔧 STEP 11: Update app.module.ts OR app.config.ts
**Choose based on your project setup:**

### If using NgModules (Traditional):
**File Location:** `src/app/app.module.ts`

**Add to imports array:**
```typescript
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { AuthInterceptor } from './core/interceptors/auth.interceptor';

@NgModule({
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    // ... other imports
  ],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ]
})
export class AppModule { }
```

### If using Standalone Components:
**File Location:** `src/main.ts`

**Replace the bootstrapApplication call with:**

```typescript
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors, HTTP_INTERCEPTORS } from '@angular/common/http';
import { importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { AuthInterceptor } from './app/core/interceptors/auth.interceptor';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    }
  ]
}).catch(err => console.error(err));
```

---

## 🔧 STEP 12: Update Environment Configuration
**File Location:** `src/environments/environment.ts`

**Add/Update these properties:**

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000'  // Change port if your backend runs on different port
};
```

**File Location:** `src/environments/environment.prod.ts`

**Update for production:**

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-api-domain.com'
};
```

---

## 🔧 STEP 13: Update App Routing
**File Location:** `src/app/app.routes.ts` (or `app-routing.module.ts`)

**Add these routes:**

```typescript
import { Routes } from '@angular/router';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AuthGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // Auth routes (no guard needed)
  {
    path: 'auth',
    children: [
      { path: 'login', component: LoginComponent },
      { path: 'register', component: RegisterComponent }
    ]
  },

  // Protected routes (require authentication)
  {
    path: 'employees',
    loadComponent: () => import('./features/employees/employees.component').then(m => m.EmployeesComponent),
    canActivate: [AuthGuard]
  },

  {
    path: 'leave-requests',
    loadComponent: () => import('./features/leave-requests/leave-requests.component').then(m => m.LeaveRequestsComponent),
    canActivate: [AuthGuard]
  },

  // Manager-only route example
  {
    path: 'leave-approvals',
    loadComponent: () => import('./features/leave-approvals/leave-approvals.component').then(m => m.LeaveApprovalsComponent),
    canActivate: [AuthGuard],
    data: { roles: ['Manager'] }
  },

  // Redirect to login by default
  { path: '', redirectTo: '/leave-requests', pathMatch: 'full' },
  { path: '**', redirectTo: '/auth/login' }
];
```

---

## 🔧 STEP 14: Update App Component with Navigation
**File Location:** `src/app/app.component.ts`

**Update with these changes:**

```typescript
import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { UserDto } from './core/models/auth.models';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'Smart Leave Management';
  currentUser$: Observable<UserDto | null>;
  isAuthenticated$: Observable<boolean>;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {
    this.currentUser$ = this.authService.currentUser$;
    this.isAuthenticated$ = this.authService.isAuthenticated$;
  }

  ngOnInit(): void {}

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }

  isManager(): boolean {
    return this.authService.currentUserValue?.role === 'Manager';
  }
}
```

---

## 🔧 STEP 15: Update App Component Template
**File Location:** `src/app/app.component.html`

**Replace entire file with:**

```html
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
  <div class="container-fluid">
    <a class="navbar-brand" routerLink="/">
      <strong>{{ title }}</strong>
    </a>
    <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
      <span class="navbar-toggler-icon"></span>
    </button>
    <div class="collapse navbar-collapse" id="navbarNav">
      <ul class="navbar-nav ms-auto">
        <ng-container *ngIf="isAuthenticated$ | async">
          <li class="nav-item">
            <a class="nav-link" routerLink="/leave-requests" routerLinkActive="active">
              Leave Requests
            </a>
          </li>
          <li class="nav-item" *ngIf="isManager()">
            <a class="nav-link" routerLink="/leave-approvals" routerLinkActive="active">
              Approvals
            </a>
          </li>
          <li class="nav-item dropdown">
            <a class="nav-link dropdown-toggle" href="#" id="userDropdown" role="button" data-bs-toggle="dropdown">
              <span *ngIf="currentUser$ | async as user">{{ user.username }}</span>
            </a>
            <ul class="dropdown-menu dropdown-menu-end" aria-labelledby="userDropdown">
              <li>
                <a class="dropdown-item" href="#">Profile</a>
              </li>
              <li>
                <hr class="dropdown-divider">
              </li>
              <li>
                <a class="dropdown-item" (click)="logout()" style="cursor: pointer;">
                  Logout
                </a>
              </li>
            </ul>
          </li>
        </ng-container>

        <ng-container *ngIf="!(isAuthenticated$ | async)">
          <li class="nav-item">
            <a class="nav-link" routerLink="/auth/login">Login</a>
          </li>
          <li class="nav-item">
            <a class="nav-link" routerLink="/auth/register">Register</a>
          </li>
        </ng-container>
      </ul>
    </div>
  </div>
</nav>

<main class="py-4">
  <router-outlet></router-outlet>
</main>
```

---

## 🔧 STEP 16: Update App Component Styles
**File Location:** `src/app/app.component.css`

**Add these styles:**

```css
:host {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

nav {
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

main {
  flex: 1;
  background-color: #f5f5f5;
}

.navbar-brand {
  font-size: 1.3rem;
}

.nav-link {
  transition: color 0.3s;
}

.nav-link:hover {
  color: #0d6efd !important;
}

.nav-link.active {
  color: #0d6efd !important;
  font-weight: 600;
}
```

---

## 📝 SUMMARY - Files to Create

| File Location | Purpose |
|---|---|
| `src/app/core/models/auth.models.ts` | Auth interfaces & types |
| `src/app/core/services/auth.service.ts` | Authentication service |
| `src/app/core/interceptors/auth.interceptor.ts` | HTTP interceptor for JWT |
| `src/app/core/guards/auth.guard.ts` | Route protection guard |
| `src/app/features/auth/login/login.component.ts` | Login component logic |
| `src/app/features/auth/login/login.component.html` | Login template |
| `src/app/features/auth/login/login.component.css` | Login styles |
| `src/app/features/auth/register/register.component.ts` | Register component logic |
| `src/app/features/auth/register/register.component.html` | Register template |
| `src/app/features/auth/register/register.component.css` | Register styles |

## 🔄 Files to Update

| File Location | Action |
|---|---|
| `src/app/app.component.ts` | Add user navigation |
| `src/app/app.component.html` | Add navbar with auth flows |
| `src/app/app.component.css` | Add navbar styles |
| `src/app/app.routes.ts` | Add auth routes + guard |
| `src/environments/environment.ts` | Set API URL |
| `src/environments/environment.prod.ts` | Production API URL |
| `src/main.ts` OR `src/app/app.module.ts` | Register interceptor |

---

## ✅ After Implementation

1. Run: `ng serve`
2. Navigate to `http://localhost:4200`
3. Register a new user (Employee or Manager role)
4. Login with credentials
5. Access protected routes
6. Managers can see additional features

