# Phase 4: Visual Architecture & Implementation Details

## 🏗️ SYSTEM ARCHITECTURE DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                     Angular Frontend                            │
│  (LeaveApplyComponent, LeaveListComponent, ApprovalComponent)   │
└──────────────────────────────┬──────────────────────────────────┘
                               │ HTTP Requests with JWT
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core Web API                           │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │         LeavesController                                │  │
│  │  - ApplyForLeave()    [Employee]                        │  │
│  │  - GetMyLeaves()      [Employee]                        │  │
│  │  - GetPendingLeaves() [Manager]                         │  │
│  │  - ApproveLeave()     [Manager]                         │  │
│  │  - RejectLeave()      [Manager]                         │  │
│  └──────────────────┬───────────────────────────────────────┘  │
│                     │ Uses                                      │
│                     ▼                                           │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │         ILeaveService / LeaveService                    │  │
│  │                                                         │  │
│  │  ✓ ApplyForLeaveAsync()                                │  │
│  │    - Validate date range                              │  │
│  │    - Check overlapping leaves                         │  │
│  │    - Create pending leave request                     │  │
│  │                                                         │  │
│  │  ✓ HasOverlappingLeaveAsync()                          │  │
│  │    - Check for date conflicts                         │  │
│  │    - Exclude rejected leaves                          │  │
│  │                                                         │  │
│  │  ✓ ApproveLeaveAsync()                                 │  │
│  │    - Verify leave exists                              │  │
│  │    - Check not already processed                       │  │
│  │    - Prevent self-approval                            │  │
│  │    - Update status to Approved                        │  │
│  │                                                         │  │
│  │  ✓ RejectLeaveAsync()                                  │  │
│  │    - Same checks as approve                           │  │
│  │    - Update status to Rejected                        │  │
│  │    - Store rejection reason                           │  │
│  └──────────────────┬───────────────────────────────────────┘  │
│                     │ Uses                                      │
│                     ▼                                           │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │      ApplicationDbContext (Entity Framework)            │  │
│  │                                                         │  │
│  │  DbSet<LeaveRequest>                                   │  │
│  │  DbSet<Employee>                                       │  │
│  │  DbSet<User>                                           │  │
│  └──────────────────┬───────────────────────────────────────┘  │
│                     │                                           │
└─────────────────────┼───────────────────────────────────────────┘
                      │ CRUD Operations
                      ▼
        ┌──────────────────────────────┐
        │   SQL Server Database        │
        │                              │
        │  Tables:                     │
        │  - LeaveRequests             │
        │  - Employees                 │
        │  - Users                     │
        │  - Migrations History        │
        └──────────────────────────────┘
```

---

## 📊 DATA MODEL - LeaveRequest Entity

```csharp
public class LeaveRequest
{
    // Existing fields
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; }
    
    // NEW fields for approval workflow
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public int? ApprovedBy { get; set; }           // FK to User.Id
    public User? ApprovedByUser { get; set; }      // Navigation to manager
    public DateTime? ApprovedDate { get; set; }    // When approved/rejected
    public string RejectionReason { get; set; }    // Why rejected
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}

public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
```

---

## 🔄 LEAVE LIFECYCLE STATE DIAGRAM

```
                    ┌──────────────┐
                    │   Created    │
                    │  by Employee │
                    └──────┬───────┘
                           │
                           ▼
                   ┌──────────────────┐
                   │  Status=Pending  │◄─────────────┐
                   │                  │              │
                   │ Visible to:      │ Modifications
                   │ - Employee       │ allowed before
                   │ - All Managers   │ manager reviews
                   └────┬────────┬────┘              │
                        │        │                   │
                        │        └───────────────────┘
                        │
          ┌─────────────┴─────────────┐
          │                           │
          ▼                           ▼
    ┌──────────────┐          ┌──────────────┐
    │  APPROVED    │          │  REJECTED    │
    │ by Manager   │          │  by Manager  │
    │              │          │              │
    │ Fields set:  │          │ Fields set:  │
    │ - Status     │          │ - Status     │
    │ - ApprovedBy │          │ - ApprovedBy │
    │ - Date       │          │ - Date       │
    │              │          │ - Reason     │
    └──────────────┘          └──────────────┘
         │                          │
         │                          │
         └──────────┬───────────────┘
                    │
                    ▼
         FINAL STATE (Immutable)
      Cannot be changed again
```

---

## 🔐 AUTHORIZATION MATRIX - Who Can Do What?

```
┌──────────────────────┬──────────────┬──────────────┐
│      Endpoint        │   Employee   │   Manager    │
├──────────────────────┼──────────────┼──────────────┤
│ POST /leaves/apply   │ ✅ Own only  │ ✅ Own only  │
├──────────────────────┼──────────────┼──────────────┤
│ GET /leaves/my       │ ✅ Own       │ ✅ Own       │
├──────────────────────┼──────────────┼──────────────┤
│ GET /leaves/pending  │ ❌ DENIED    │ ✅ All       │
├──────────────────────┼──────────────┼──────────────┤
│ PUT /leaves/{id}/    │              │              │
│     approve          │ ❌ DENIED    │ ✅ Others*   │
├──────────────────────┼──────────────┼──────────────┤
│ PUT /leaves/{id}/    │              │              │
│     reject           │ ❌ DENIED    │ ✅ Others*   │
└──────────────────────┴──────────────┴──────────────┘

* = Cannot approve/reject your own leave request
```

---

## 🔍 VALIDATION RULES - DETAILED LOGIC

### Rule 1: Date Range Validation
```
REQUIREMENT: StartDate must be strictly before EndDate

VALIDATION:
if (request.StartDate >= request.EndDate)
    throw InvalidOperationException("Start date must be before end date");

EXAMPLE:
✅ Valid:   StartDate: 2025-02-01, EndDate: 2025-02-05
❌ Invalid: StartDate: 2025-02-05, EndDate: 2025-02-05
❌ Invalid: StartDate: 2025-02-05, EndDate: 2025-02-01
```

### Rule 2: Overlapping Leave Detection
```
REQUIREMENT: No employee can have overlapping leaves (except Rejected ones)

LOGIC:
var overlapping = _context.LeaveRequests
    .Where(lr => lr.EmployeeId == employeeId
        && lr.Status != LeaveStatus.Rejected      // Ignore rejected
        && lr.Id != leaveIdToExclude              // Exclude current when editing
        && !(lr.EndDate < startDate               // Check overlap
            || lr.StartDate > endDate))
    .AnyAsync();

OVERLAP CONDITION (Visual):
Existing Leave:    [====Request 1====]
New Request:  [====Request 2====]
              ❌ OVERLAPS

Acceptable:
Existing Leave:    [====Request 1====]
New Request:                           [====Request 2====]
                   ✅ OK (adjacent dates)

Overlapping Cases:
Case 1:      [===Request 1===]
                   [===Request 2===]        ❌ Overlaps

Case 2:      [===Request 1===]
             [===Request 2===]              ❌ Overlaps (partial)

Case 3:      [===Request 1===]
        [===Request 2===]                   ✅ OK (no overlap)
```

### Rule 3: Manager Self-Approval Prevention
```
REQUIREMENT: Manager cannot approve their own leave request

LOGIC:
if (leave.EmployeeId == currentManagerId)
    throw UnauthorizedAccessException(
        "You cannot approve your own leave");

SCENARIO:
- Manager Sarah creates a leave request (EmployeeId = Sarah's ID)
- Manager Sarah tries to approve it
- System throws error: "You cannot approve your own leave"
- Another manager (or admin) must approve Sarah's request
```

### Rule 4: Immutable Status Check
```
REQUIREMENT: Cannot re-approve or re-reject already processed leaves

LOGIC:
if (leave.Status != LeaveStatus.Pending)
    throw InvalidOperationException(
        "Cannot process leave that is already processed");

EXAMPLES:
❌ Cannot approve a leave that's already Approved
❌ Cannot reject a leave that's already Rejected
❌ Cannot approve a leave that's already Rejected
✅ Can only process leaves in Pending state
```

### Rule 5: Rejection Reason Required
```
REQUIREMENT: When rejecting, a reason must be provided

LOGIC:
if (string.IsNullOrWhiteSpace(rejectionReason))
    throw ArgumentException("Rejection reason is required");

UI SHOULD:
- Show text area for rejection reason
- Make it required in validation
- Display reason to employee in notification
```

---

## 📡 REQUEST/RESPONSE FLOW EXAMPLES

### Example 1: Apply for Leave (Happy Path)

```
REQUEST:
POST /api/leaves/apply
Authorization: Bearer eyJhbGc...
Content-Type: application/json

{
  "employeeId": 1,
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "reason": "Vacation at home"
}

PROCESSING:
1. Verify JWT token valid ✓
2. Extract userId from token = 1
3. Validate startDate < endDate ✓
4. Check for overlapping leaves ✓ (none found)
5. Create LeaveRequest entity
   - Status = Pending
   - EmployeeId = 1
   - CreatedDate = 2025-01-25T10:00:00Z
6. Save to database
7. Map to LeaveDetailDto

RESPONSE (201 Created):
{
  "id": 1,
  "employeeId": 1,
  "employeeName": "John Doe",
  "employeeEmail": "john@company.com",
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "reason": "Vacation at home",
  "status": "Pending",
  "approvedBy": null,
  "approvedByUserName": null,
  "approvedDate": null,
  "rejectionReason": null,
  "createdDate": "2025-01-25T10:00:00Z",
  "updatedDate": null
}
```

### Example 2: Apply for Leave (Validation Error)

```
REQUEST:
POST /api/leaves/apply
Authorization: Bearer eyJhbGc...

{
  "employeeId": 1,
  "startDate": "2025-02-05",
  "endDate": "2025-02-05",  // ← INVALID: same as start date
  "reason": "Vacation"
}

PROCESSING:
1. Validate startDate < endDate
2. FAILS: 2025-02-05 >= 2025-02-05 ✗

RESPONSE (400 Bad Request):
{
  "success": false,
  "message": "Start date must be before end date",
  "data": null
}
```

### Example 3: Apply for Leave (Overlap Error)

```
REQUEST:
POST /api/leaves/apply
Authorization: Bearer eyJhbGc...

{
  "employeeId": 1,
  "startDate": "2025-02-03",
  "endDate": "2025-02-07",  // ← Overlaps with 2025-02-01 to 2025-02-05
  "reason": "Vacation"
}

DATABASE STATE (before request):
LeaveRequest #1:
- EmployeeId: 1
- StartDate: 2025-02-01
- EndDate: 2025-02-05
- Status: Pending

PROCESSING:
1. Check for overlapping leaves
2. Query finds LeaveRequest #1
3. Check overlap: 2025-02-05 < 2025-02-03? NO
                  2025-02-01 > 2025-02-07? NO
4. Overlap detected ✗

RESPONSE (400 Bad Request):
{
  "success": false,
  "message": "Overlapping leave exists for this employee",
  "data": null
}
```

### Example 4: Approve Leave (Happy Path)

```
REQUEST:
PUT /api/leaves/1/approve
Authorization: Bearer eyJhbGc... (Manager Sarah's token)

PROCESSING:
1. Verify Manager token ✓
2. Extract userId = 2 (Sarah)
3. Fetch LeaveRequest #1
   - EmployeeId: 1 (John)
   - Status: Pending
4. Check not already processed: Pending ✓
5. Check not own leave: 1 != 2 ✓
6. Update fields:
   - Status = Approved
   - ApprovedBy = 2 (Sarah)
   - ApprovedDate = 2025-01-25T11:00:00Z
   - UpdatedDate = 2025-01-25T11:00:00Z
7. Save to database
8. Map to LeaveDetailDto

RESPONSE (200 OK):
{
  "success": true,
  "message": "Leave approved successfully",
  "data": {
    "id": 1,
    "employeeId": 1,
    "employeeName": "John Doe",
    "status": "Approved",
    "approvedBy": 2,
    "approvedByUserName": "Sarah Manager",
    "approvedDate": "2025-01-25T11:00:00Z",
    "rejectionReason": null,
    "createdDate": "2025-01-25T10:00:00Z",
    "updatedDate": "2025-01-25T11:00:00Z"
  }
}
```

### Example 5: Reject Leave (Happy Path)

```
REQUEST:
PUT /api/leaves/2/reject
Authorization: Bearer eyJhbGc... (Manager Sarah's token)
Content-Type: application/json

{
  "rejectionReason": "Insufficient team coverage that week"
}

PROCESSING:
1. Verify Manager token ✓
2. Extract userId = 2 (Sarah)
3. Fetch LeaveRequest #2
4. Check not already processed ✓
5. Check not own leave ✓
6. Update fields:
   - Status = Rejected
   - ApprovedBy = 2
   - ApprovedDate = 2025-01-25T12:00:00Z
   - RejectionReason = "Insufficient team coverage that week"
   - UpdatedDate = 2025-01-25T12:00:00Z
7. Save to database

RESPONSE (200 OK):
{
  "success": true,
  "message": "Leave rejected successfully",
  "data": {
    "id": 2,
    "employeeId": 1,
    "employeeName": "John Doe",
    "status": "Rejected",
    "approvedBy": 2,
    "approvedByUserName": "Sarah Manager",
    "approvedDate": "2025-01-25T12:00:00Z",
    "rejectionReason": "Insufficient team coverage that week",
    "createdDate": "2025-01-25T10:30:00Z",
    "updatedDate": "2025-01-25T12:00:00Z"
  }
}
```

---

## 🚨 ERROR SCENARIOS

### Scenario 1: Manager Tries to Approve Own Leave

```
Manager Sarah (ID=2) has pending leave request (ID=1)
Sarah tries: PUT /api/leaves/1/approve

ERROR RESPONSE (403 Forbidden):
{
  "success": false,
  "message": "You cannot approve your own leave"
}
```

### Scenario 2: Employee Tries to Approve Leave

```
Employee John (ID=1) tries: PUT /api/leaves/1/approve
Authorization: Bearer <employee_john_token>

ERROR RESPONSE (403 Forbidden):
- AuthorizeAttribute rejects due to [Authorize(Roles = "Manager")]
HTTP Status: 403 Forbidden
```

### Scenario 3: Try to Approve Already Approved Leave

```
LeaveRequest #1 already has Status = Approved
Manager Sarah tries: PUT /api/leaves/1/approve

ERROR RESPONSE (400 Bad Request):
{
  "success": false,
  "message": "Cannot process leave that is already processed"
}
```

---

## 📈 PERFORMANCE CONSIDERATIONS

### Index for Queries
```sql
-- Create index for common queries
CREATE INDEX IX_LeaveRequests_EmployeeId_Status 
  ON LeaveRequests(EmployeeId, Status);

-- This optimizes:
-- - Getting pending leaves by employee
-- - Getting approved/rejected leaves
-- - Overlap detection
```

### Query Optimization
```csharp
// GOOD: Uses index efficiently
var pending = _context.LeaveRequests
    .Where(lr => lr.EmployeeId == empId && lr.Status == LeaveStatus.Pending)
    .ToListAsync();

// BAD: Loads all data to memory then filters
var pending = _context.LeaveRequests
    .ToList()  // ← WRONG: loads all rows to memory
    .Where(lr => lr.EmployeeId == empId && lr.Status == LeaveStatus.Pending)
    .ToList();
```

---

## 🔄 ENTITY RELATIONSHIPS

```
LeaveRequest
├── Employee (FK: EmployeeId)
│   ├── Id
│   ├── Name
│   └── Email
│
└── ApprovedByUser (FK: ApprovedBy, nullable)
    ├── Id
    ├── Username
    ├── Email
    └── Role (should be "Manager" for approval)
```

---

## 📝 DATABASE SCHEMA

```sql
CREATE TABLE [dbo].[LeaveRequests] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [EmployeeId] INT NOT NULL,
    [StartDate] DATETIME2 NOT NULL,
    [EndDate] DATETIME2 NOT NULL,
    [Reason] NVARCHAR(MAX) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0,  -- 0=Pending, 1=Approved, 2=Rejected
    [ApprovedBy] INT NULL,
    [ApprovedDate] DATETIME2 NULL,
    [RejectionReason] NVARCHAR(MAX) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedDate] DATETIME2 NULL,
    
    FOREIGN KEY ([EmployeeId]) 
        REFERENCES [dbo].[Employees]([Id]),
    FOREIGN KEY ([ApprovedBy]) 
        REFERENCES [dbo].[Users]([Id])
);

CREATE INDEX IX_LeaveRequests_EmployeeId_Status 
    ON [dbo].[LeaveRequests]([EmployeeId], [Status]);
```

