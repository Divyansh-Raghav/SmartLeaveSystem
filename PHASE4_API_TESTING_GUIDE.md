# Phase 4: Leave Approval Workflow - API Testing Guide

## 📋 SETUP STEPS

### 1. Run Database Migration
Before testing, apply the migration to update your database schema:

```bash
# In Package Manager Console or Terminal
dotnet ef database update
```

This will add the new columns and indexes to the `LeaveRequests` table.

---

## 🔐 TEST USERS

For testing, you'll need two users: an Employee and a Manager.

**Employee User:**
```
Username: john_employee
Email: john@company.com
Password: Employee@123
Role: Employee
```

**Manager User:**
```
Username: jane_manager
Email: jane@company.com
Password: Manager@123
Role: Manager
```

### Register Test Users

**POST /api/auth/register**
```json
{
  "username": "john_employee",
  "email": "john@company.com",
  "password": "Employee@123",
  "role": "Employee"
}

{
  "username": "jane_manager",
  "email": "jane@company.com",
  "password": "Manager@123",
  "role": "Manager"
}
```

### Get JWT Tokens

**POST /api/auth/login**
```json
{
  "email": "john@company.com",
  "password": "Employee@123"
}
```

Response:
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "john_employee",
    "email": "john@company.com",
    "role": "Employee"
  }
}
```

Save the `token` value for use in subsequent requests.

---

## 🧪 API ENDPOINT TESTS

### 1. Apply for Leave
**POST /api/leaves/apply**

**Authorization:** Bearer token (Employee)

**Request Body:**
```json
{
  "employeeId": 1,
  "startDate": "2025-02-15",
  "endDate": "2025-02-20",
  "reason": "Annual vacation"
}
```

**Expected Response (201 Created):**
```json
{
  "success": true,
  "message": "Leave applied successfully",
  "data": {
    "id": 1,
    "employeeId": 1,
    "employee": null,
    "startDate": "2025-02-15T00:00:00",
    "endDate": "2025-02-20T00:00:00",
    "reason": "Annual vacation",
    "status": "Pending",
    "approvedBy": null,
    "approvedByUser": null,
    "approvedDate": null,
    "rejectionReason": null,
    "createdDate": "2025-01-25T12:30:00.1234567Z",
    "updatedDate": null
  }
}
```

**Test Case: Invalid Date Range**
```json
{
  "employeeId": 1,
  "startDate": "2025-02-20",
  "endDate": "2025-02-15",
  "reason": "Invalid dates"
}
```

Expected Response (400 Bad Request):
```json
{
  "success": false,
  "message": "Start date must be before end date"
}
```

**Test Case: Overlapping Leaves**

Apply two overlapping leave requests:
```json
{
  "employeeId": 1,
  "startDate": "2025-02-15",
  "endDate": "2025-02-20",
  "reason": "First leave"
}
```

Then try to apply an overlapping one:
```json
{
  "employeeId": 1,
  "startDate": "2025-02-18",
  "endDate": "2025-02-25",
  "reason": "Overlapping leave"
}
```

Expected Response (400 Bad Request):
```json
{
  "success": false,
  "message": "Leave request overlaps with an existing leave"
}
```

---

### 2. Get My Leaves
**GET /api/leaves/my**

**Authorization:** Bearer token (Employee)

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Retrieved 1 leave(s)",
  "data": [
    {
      "id": 1,
      "employeeId": 1,
      "employee": {
        "id": 1,
        "name": "John Doe",
        "email": "john@company.com"
      },
      "startDate": "2025-02-15T00:00:00",
      "endDate": "2025-02-20T00:00:00",
      "reason": "Annual vacation",
      "status": "Pending",
      "approvedBy": null,
      "approvedByUser": null,
      "approvedDate": null,
      "rejectionReason": null,
      "createdDate": "2025-01-25T12:30:00Z",
      "updatedDate": null
    }
  ]
}
```

**Authorization Test:** Try without a token
- Expected Response (401 Unauthorized):
```json
{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Authorization header was not provided or was invalid"
}
```

---

### 3. Get Pending Leaves (Manager Only)
**GET /api/leaves/pending**

**Authorization:** Bearer token (Manager only)

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Retrieved 1 pending leave(s)",
  "data": [
    {
      "id": 1,
      "employeeId": 1,
      "employee": {
        "id": 1,
        "name": "John Doe",
        "email": "john@company.com"
      },
      "startDate": "2025-02-15T00:00:00",
      "endDate": "2025-02-20T00:00:00",
      "reason": "Annual vacation",
      "status": "Pending",
      "approvedBy": null,
      "approvedByUser": null,
      "approvedDate": null,
      "rejectionReason": null,
      "createdDate": "2025-01-25T12:30:00Z",
      "updatedDate": null
    }
  ]
}
```

**Authorization Test: Employee tries to access**
- Expected Response (403 Forbidden)

---

### 4. Approve Leave (Manager Only)
**PUT /api/leaves/{id}/approve**

**Authorization:** Bearer token (Manager)

**Request Body:** {} (empty)

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Leave approved successfully",
  "data": {
    "id": 1,
    "employeeId": 1,
    "employee": {
      "id": 1,
      "name": "John Doe",
      "email": "john@company.com"
    },
    "startDate": "2025-02-15T00:00:00",
    "endDate": "2025-02-20T00:00:00",
    "reason": "Annual vacation",
    "status": "Approved",
    "approvedBy": 2,
    "approvedByUser": {
      "id": 2,
      "username": "jane_manager",
      "email": "jane@company.com"
    },
    "approvedDate": "2025-01-25T13:45:00Z",
    "rejectionReason": null,
    "createdDate": "2025-01-25T12:30:00Z",
    "updatedDate": "2025-01-25T13:45:00Z"
  }
}
```

**Test Case: Manager tries to approve own leave**

First, create a leave for the manager (employeeId = manager's user id):
```json
{
  "employeeId": 2,
  "startDate": "2025-03-01",
  "endDate": "2025-03-05",
  "reason": "Manager's vacation"
}
```

Then try to approve it with manager token:
- Expected Response (403 Forbidden)

**Test Case: Leave not found**
```
PUT /api/leaves/9999/approve
```
- Expected Response (404 Not Found)

**Test Case: Leave already processed**
Approve the same leave twice:
- First request: Success (200)
- Second request: Expected (400 Bad Request)
```json
{
  "success": false,
  "message": "Cannot approve leave that is already Approved"
}
```

---

### 5. Reject Leave (Manager Only)
**PUT /api/leaves/{id}/reject**

**Authorization:** Bearer token (Manager)

**Request Body:**
```json
{
  "rejectionReason": "Insufficient coverage during this period"
}
```

**Expected Response (200 OK):**
```json
{
  "success": true,
  "message": "Leave rejected successfully",
  "data": {
    "id": 1,
    "employeeId": 1,
    "employee": {
      "id": 1,
      "name": "John Doe",
      "email": "john@company.com"
    },
    "startDate": "2025-02-15T00:00:00",
    "endDate": "2025-02-20T00:00:00",
    "reason": "Annual vacation",
    "status": "Rejected",
    "approvedBy": 2,
    "approvedByUser": {
      "id": 2,
      "username": "jane_manager",
      "email": "jane@company.com"
    },
    "approvedDate": "2025-01-25T14:00:00Z",
    "rejectionReason": "Insufficient coverage during this period",
    "createdDate": "2025-01-25T12:30:00Z",
    "updatedDate": "2025-01-25T14:00:00Z"
  }
}
```

**Test Case: Missing rejection reason**
```json
{
  "rejectionReason": ""
}
```
- Expected Response (400 Bad Request)

**Test Case: Manager tries to reject own leave**
- Expected Response (403 Forbidden)

---

## ✅ SUMMARY OF TEST SCENARIOS

| Scenario | Endpoint | Expected Status | Notes |
|----------|----------|-----------------|-------|
| Valid apply | POST /leaves/apply | 201 | Employee applies for leave |
| Invalid dates | POST /leaves/apply | 400 | Start date >= End date |
| Overlapping leaves | POST /leaves/apply | 400 | Existing non-rejected leave overlaps |
| Get my leaves | GET /leaves/my | 200 | Employee views own leaves |
| No authorization | Any endpoint | 401 | Missing/invalid token |
| Get pending leaves | GET /leaves/pending | 200 | Manager only |
| Employee access pending | GET /leaves/pending | 403 | Employee tries manager endpoint |
| Approve leave | PUT /leaves/{id}/approve | 200 | Manager approves |
| Manager approves own | PUT /leaves/{id}/approve | 403 | Self-approval forbidden |
| Approve non-existent | PUT /leaves/{id}/approve | 404 | Leave ID not found |
| Approve already approved | PUT /leaves/{id}/approve | 400 | Cannot re-approve |
| Reject leave | PUT /leaves/{id}/reject | 200 | Manager rejects with reason |
| Reject without reason | PUT /leaves/{id}/reject | 400 | Rejection reason required |

---

## 🔧 POSTMAN COLLECTION TEMPLATE

Use these settings for all requests:

**Headers:**
```
Authorization: Bearer {token}
Content-Type: application/json
```

**Environment Variables:**
```
base_url: http://localhost:5000
employee_token: {paste employee jwt token}
manager_token: {paste manager jwt token}
leave_id: {the id returned from apply leave}
```

---

## 📝 NOTES

- All timestamps are in UTC
- Employee can only apply for their own leaves (EmployeeId must match current user)
- Manager cannot approve/reject their own leaves
- Once approved or rejected, a leave cannot be modified
- Rejected leaves do not prevent overlapping new applications
- Database migration must be applied before testing

