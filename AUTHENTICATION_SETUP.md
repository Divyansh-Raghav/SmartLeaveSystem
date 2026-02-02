# JWT Authentication Setup - Smart Leave Management System

## ✅ What's Been Implemented

### Backend Changes:
1. **User Model** (`Models/User.cs`) - Stores user credentials with roles
2. **AuthController** (`Controllers/AuthController.cs`) - Register & Login endpoints
3. **TokenService** (`Services/TokenService.cs`) - JWT token generation
4. **Authorization** - LeaveRequests controller now requires authentication
5. **Role-based Access** - `GetAll()` restricted to Manager role
6. **JWT Configuration** - Added to `appsettings.json`

---

## 🗄️ Database Migration Required

Since you've added a new `User` table, run this migration:

```bash
cd SmartLeaveManagement
dotnet ef migrations add AddUserTable
dotnet ef database update
```

This will create the `Users` table in your SQL Server database.

---

## 📋 API Testing - Sample Payloads

### 1. Register a New User

**Endpoint:** `POST /api/auth/register`

**Request Body:**
```json
{
  "username": "john_emp",
  "email": "john@company.com",
  "password": "SecurePassword123!",
  "role": "Employee"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "message": "Registration successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "john_emp",
    "email": "john@company.com",
    "role": "Employee"
  }
}
```

### 2. Register a Manager

**Endpoint:** `POST /api/auth/register`

**Request Body:**
```json
{
  "username": "manager_sarah",
  "email": "sarah@company.com",
  "password": "SecurePassword123!",
  "role": "Manager"
}
```

### 3. Login User

**Endpoint:** `POST /api/auth/login`

**Request Body:**
```json
{
  "email": "john@company.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
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

## 🔐 Protected Endpoints

### LeaveRequests - Now Protected

All endpoints require a valid JWT token in the `Authorization` header.

**Header Format:**
```
Authorization: Bearer <your_jwt_token_here>
```

#### Example: Get All Leave Requests (Manager Only)
```bash
curl -X GET http://localhost:5000/api/leaverequests \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

**Rules:**
- ✅ `GET /api/leaverequests` - **Manager only**
- ✅ `GET /api/leaverequests/{id}` - Authorized users
- ✅ `GET /api/leaverequests/employee/{employeeId}` - Authorized users
- ✅ `POST /api/leaverequests` - Authorized users (create own request)
- ✅ `PUT /api/leaverequests/{id}` - Authorized users
- ✅ `DELETE /api/leaverequests/{id}` - Authorized users

---

## ⚙️ JWT Configuration

Located in `appsettings.json`:

```json
"Jwt": {
  "Key": "your-super-secret-key-that-is-at-least-32-characters-long-for-hmac256!",
  "Issuer": "SmartLeaveManagement",
  "Audience": "SmartLeaveManagementClient",
  "ExpiryHours": "2"
}
```

**⚠️ Important:** Change the `Key` to a strong, unique secret in production!

---

## 🧪 Testing with Postman or Swagger

### Option 1: Swagger UI (Easy)
1. Run the application
2. Navigate to `https://localhost:5001/swagger`
3. Use Swagger to test `/api/auth/register` and `/api/auth/login`
4. Copy the token from response
5. Click the **Authorize** button and paste: `Bearer <token>`
6. Test protected endpoints

### Option 2: Postman
1. Create a new POST request to `http://localhost:5000/api/auth/login`
2. Set body to raw JSON with login credentials
3. Send and copy the token
4. For protected endpoints, go to **Headers** tab
5. Add: `Authorization: Bearer <token>`

---

## 🚀 Next Steps - Frontend (Angular)

1. **Create Auth Service** - Handle login/register API calls
2. **Create Login Component** - Form for email/password
3. **Create Register Component** - Form for new user signup
4. **Store JWT Token** - Save in localStorage after login
5. **Add Authorization Header** - Automatically add token to all requests
6. **Route Guards** - Protect Angular routes that need authentication
7. **Role-based UI** - Show/hide features based on user role

---

## 📝 Notes

- Passwords are hashed using PBKDF2 with SHA256
- JWT tokens expire after 2 hours (configurable)
- Users must re-login to get a new token
- Tokens contain: User ID, Email, Username, Role (in claims)
- No endpoints are public except `/api/auth/register` and `/api/auth/login`

