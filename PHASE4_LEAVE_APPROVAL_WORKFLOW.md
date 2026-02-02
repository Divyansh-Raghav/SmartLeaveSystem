# Phase 4: Leave Approval Workflow - Implementation Guide
## Smart Leave Management System

---

## 📋 OVERVIEW

This phase extends the existing LeaveRequest CRUD with an approval workflow where:
- **Employees** apply for leave (status = Pending)
- **Managers** view all team leaves and approve/reject them
- **Validations** prevent overlapping dates and invalid date ranges
- **Authorization** ensures employees only see their own leaves

---

## 🎯 BUSINESS LOGIC FLOW

```
Employee Creates Leave Request
    ↓
Status = Pending
    ↓
Manager Reviews Pending Leaves
    ↓
Manager Approves OR Rejects
    ↓
Status Updated to Approved OR Rejected
    ↓
Employee Receives Notification (Future Phase)
```

---

## 📁 FILES TO CREATE/UPDATE

### Create New Files:
1. `Models/LeaveStatus.cs` - Enum for leave statuses
2. `Services/LeaveService.cs` - Business logic layer
3. `DTOs/LeaveApprovalDto.cs` - Approval request/response models

### Update Existing Files:
1. `Models/LeaveRequest.cs` - Add status, approval fields
2. `Data/ApplicationDbContext.cs` - Add configurations if needed
3. `Controllers/LeaveRequestsController.cs` - New endpoints + refactor
4. `Program.cs` - Register LeaveService

### Database:
1. Create new migration for LeaveRequest changes

---

## ⚙️ DETAILED SPECIFICATIONS

### LeaveStatus Enum Values:
- `Pending` = 0
- `Approved` = 1
- `Rejected` = 2

### Updated LeaveRequest Model Fields:
```csharp
- Id (existing)
- EmployeeId (existing)
- Employee (existing navigation)
- StartDate (existing)
- EndDate (existing)
- Reason (existing)

// NEW FIELDS:
- Status (enum, default Pending)
- ApprovedBy (nullable int, FK to User.Id)
- ApprovedByUser (nullable User navigation)
- ApprovedDate (nullable DateTime)
- RejectionReason (nullable string)
- CreatedDate (DateTime, default UtcNow)
- UpdatedDate (nullable DateTime)
```

### Validation Rules:
1. StartDate < EndDate
2. No overlapping leaves for same employee
3. Manager cannot approve their own leave
4. Cannot approve/reject already processed leaves
5. Rejection requires rejection reason

### API Endpoints:

#### 1. Apply for Leave
```
POST /api/leaves/apply
Authorization: Bearer <token>

Request Body:
{
  "employeeId": 1,
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "reason": "Vacation"
}

Response: 201 Created
{
  "id": 1,
  "employeeId": 1,
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "reason": "Vacation",
  "status": "Pending",
  "createdDate": "2025-01-25T10:00:00Z"
}

Error: 400 Bad Request
{
  "success": false,
  "message": "Overlapping leave exists for this employee"
}
```

#### 2. Get My Leaves
```
GET /api/leaves/my
Authorization: Bearer <token>

Response: 200 OK
[
  {
    "id": 1,
    "employeeId": 1,
    "startDate": "2025-02-01",
    "endDate": "2025-02-05",
    "reason": "Vacation",
    "status": "Pending",
    "approvedBy": null,
    "approvedDate": null,
    "createdDate": "2025-01-25T10:00:00Z"
  }
]
```

#### 3. Get Pending Leaves (Manager Only)
```
GET /api/leaves/pending
Authorization: Bearer <token> (Manager only)

Response: 200 OK
[
  {
    "id": 1,
    "employeeId": 1,
    "employee": {
      "id": 1,
      "name": "John Doe",
      "email": "john@company.com"
    },
    "startDate": "2025-02-01",
    "endDate": "2025-02-05",
    "reason": "Vacation",
    "status": "Pending",
    "createdDate": "2025-01-25T10:00:00Z"
  }
]
```

#### 4. Approve Leave (Manager Only)
```
PUT /api/leaves/{id}/approve
Authorization: Bearer <token> (Manager only)

Request Body: {} (empty)

Response: 200 OK
{
  "success": true,
  "message": "Leave approved successfully",
  "data": {
    "id": 1,
    "status": "Approved",
    "approvedBy": 2,
    "approvedDate": "2025-01-25T11:00:00Z"
  }
}

Error: 403 Forbidden (if trying to approve own leave)
{
  "success": false,
  "message": "You cannot approve your own leave"
}
```

#### 5. Reject Leave (Manager Only)
```
PUT /api/leaves/{id}/reject
Authorization: Bearer <token> (Manager only)

Request Body:
{
  "rejectionReason": "Insufficient coverage that week"
}

Response: 200 OK
{
  "success": true,
  "message": "Leave rejected successfully",
  "data": {
    "id": 1,
    "status": "Rejected",
    "rejectionReason": "Insufficient coverage that week",
    "approvedBy": 2,
    "approvedDate": "2025-01-25T11:00:00Z"
  }
}

Error: 400 Bad Request (if already processed)
{
  "success": false,
  "message": "Cannot reject leave that is already approved"
}
```

---

## 🔐 AUTHORIZATION MATRIX

| Endpoint | Employee | Manager |
|----------|----------|---------|
| POST /api/leaves/apply | ✅ Own only | ✅ Own only |
| GET /api/leaves/my | ✅ Own leaves | ✅ Own leaves |
| GET /api/leaves/pending | ❌ | ✅ All pending |
| PUT /leaves/{id}/approve | ❌ | ✅ Others only |
| PUT /leaves/{id}/reject | ❌ | ✅ Others only |

---

## 📊 DATABASE SCHEMA CHANGES

### New/Updated Table: LeaveRequests

```sql
CREATE TABLE LeaveRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmployeeId INT NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Reason NVARCHAR(MAX) NOT NULL,
    Status INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Approved, 2=Rejected
    ApprovedBy INT NULL,
    ApprovedDate DATETIME2 NULL,
    RejectionReason NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 NULL,
    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    FOREIGN KEY (ApprovedBy) REFERENCES Users(Id)
);
```

### Index for Performance:
```sql
CREATE INDEX IX_LeaveRequests_EmployeeId_Status 
  ON LeaveRequests(EmployeeId, Status);
```

---

## 🛠️ IMPLEMENTATION STRUCTURE

```
SmartLeaveManagement/
├── Models/
│   ├── LeaveStatus.cs (NEW)
│   ├── LeaveRequest.cs (UPDATED)
│   └── Employee.cs
├── Services/
│   ├── LeaveService.cs (NEW)
│   └── TokenService.cs
├── DTOs/
│   ├── LeaveApprovalDto.cs (NEW)
│   └── AuthDto.cs
├── Controllers/
│   ├── LeavesController.cs (REFACTORED from LeaveRequestsController)
│   └── AuthController.cs
├── Data/
│   ├── ApplicationDbContext.cs (UPDATED)
│   └── Migrations/
│       └── [New migration for LeaveRequest schema]
└── Program.cs (UPDATED)
```

---

## 📝 KEY VALIDATION LOGIC

### 1. Check Date Range Validity
```csharp
if (request.StartDate >= request.EndDate)
    throw new ValidationException("Start date must be before end date");
```

### 2. Check Overlapping Leaves
```csharp
var overlapping = await _context.LeaveRequests
    .Where(lr => lr.EmployeeId == employeeId 
        && lr.Status != LeaveStatus.Rejected
        && !(lr.EndDate < startDate || lr.StartDate > endDate))
    .AnyAsync();

if (overlapping)
    throw new ValidationException("Leave request overlaps with existing leave");
```

### 3. Check Manager Cannot Approve Own Leave
```csharp
var leave = await _context.LeaveRequests.FindAsync(leaveId);
var currentUser = await GetCurrentUserFromToken();

if (leave.EmployeeId == currentUser.Id)
    throw new UnauthorizedAccessException("Cannot approve your own leave");
```

---

## 🚀 NEXT STEPS (Frontend Angular)

After backend implementation:
1. Create LeaveApplyComponent
2. Create LeaveListComponent (filtered by role)
3. Create LeaveApprovalComponent (Manager only)
4. Add approval/rejection modals
5. Update navigation for Manager-only features
6. Add status badges (Pending/Approved/Rejected)

---

## 📌 NOTES

- All dates stored as UTC in database
- API returns ISO 8601 format (YYYY-MM-DDTHH:MM:SSZ)
- Soft-delete not implemented (rejection is permanent)
- Audit trail can be enhanced with CreatedBy, UpdatedBy fields later
- Rate limiting recommended for production

