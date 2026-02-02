# 🚀 PHASE 4 QUICK START GUIDE

## Immediate Next Steps

You now have two comprehensive documents:

1. **PHASE4_LEAVE_APPROVAL_WORKFLOW.md** - Complete business requirements and specifications
2. **PHASE4_COPY_PASTE_PROMPTS.md** - Step-by-step prompts to paste into VS Code Copilot

---

## 📋 QUICK FLOW

### What You're Building:

```
Employee applies for leave (Pending status)
        ↓
Manager reviews pending leaves  
        ↓
Manager approves or rejects
        ↓
Leave status updated to Approved or Rejected
```

---

## 🎯 10 STEPS TO COMPLETE PHASE 4

### STEP 1: Create Enum
Prompt 1: Create LeaveStatus.cs with Pending, Approved, Rejected

### STEP 2: Update Model
Prompt 2: Add Status, ApprovedBy, ApprovedDate, RejectionReason, CreatedDate, UpdatedDate to LeaveRequest

### STEP 3: Create DTOs
Prompt 3: Create LeaveApprovalDto.cs with LeaveDetailDto, LeaveApprovalResponse, RejectLeaveRequest

### STEP 4: Create Service Layer
Prompt 4: Create LeaveService.cs with ApplyForLeaveAsync, HasOverlappingLeaveAsync, ApproveLeaveAsync, RejectLeaveAsync

### STEP 5: Register Service
Prompt 5: Add builder.Services.AddScoped<ILeaveService, LeaveService>() in Program.cs

### STEP 6: Configure Database
Prompt 6: Update ApplicationDbContext with FluentAPI configurations

### STEP 7: Refactor Controller
Prompt 7: Replace LeaveRequestsController with new LeavesController with approval endpoints

### STEP 8: Create Helper DTO
Prompt 7b: Create ApplyLeaveRequest DTO

### STEP 9: Create Migration & Update Database
Prompt 8: Run migrations to update SQL Server

### STEP 10: Verify Everything
Prompt 10: Build project and test endpoints

---

## 🔑 KEY ENDPOINTS SUMMARY

| Method | Endpoint | Role | Purpose |
|--------|----------|------|---------|
| POST | /api/leaves/apply | Employee | Create leave request |
| GET | /api/leaves/my | Employee | View own leaves |
| GET | /api/leaves/pending | Manager | View all pending leaves |
| PUT | /api/leaves/{id}/approve | Manager | Approve a leave |
| PUT | /api/leaves/{id}/reject | Manager | Reject a leave |

---

## 💡 KEY BUSINESS RULES

1. ✅ Employees can only see their own leaves
2. ✅ Managers can see all pending leaves
3. ✅ Managers cannot approve/reject their own leave
4. ✅ Start date must be before end date
5. ✅ No overlapping leaves for same employee
6. ✅ Rejected leaves cannot be re-approved
7. ✅ Rejection requires a reason

---

## 🏗️ FILE STRUCTURE AFTER COMPLETION

```
SmartLeaveManagement/
├── Models/
│   ├── LeaveStatus.cs ........................ NEW
│   ├── LeaveRequest.cs ....................... UPDATED (added Status, ApprovedBy, etc.)
│   ├── Employee.cs ........................... unchanged
│   └── User.cs .............................. unchanged
├── Services/
│   ├── LeaveService.cs ....................... NEW
│   └── TokenService.cs ....................... unchanged
├── DTOs/
│   ├── LeaveApprovalDto.cs ................... NEW
│   └── AuthDto.cs ........................... unchanged
├── Controllers/
│   ├── LeavesController.cs ................... NEW (refactored from LeaveRequestsController)
│   ├── AuthController.cs ..................... unchanged
│   └── EmployeesController.cs ................ unchanged
├── Data/
│   ├── ApplicationDbContext.cs ............... UPDATED (add configurations)
│   └── Migrations/
│       ├── 20260125081657_SecondCreate.cs ... existing
│       ├── 20260125082721_AddRoleAndCreatedAtToUsers.cs ... existing
│       └── [NEW]_UpdateLeaveRequestWithApprovalFields.cs
└── Program.cs ............................... UPDATED (register LeaveService)
```

---

## 🧪 TESTING WORKFLOW

### 1. Register Users
```
POST /api/auth/register (Employee: john_emp)
POST /api/auth/register (Manager: sarah_mgr)
```

### 2. Create Leave Request
```
POST /api/leaves/apply
Body: {
  "employeeId": 1,
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "reason": "Vacation"
}
```

### 3. Check My Leaves
```
GET /api/leaves/my (as john_emp)
Expected: See the leave with status "Pending"
```

### 4. Manager Reviews
```
GET /api/leaves/pending (as sarah_mgr)
Expected: See john_emp's pending leave
```

### 5. Approve Leave
```
PUT /api/leaves/1/approve (as sarah_mgr)
Expected: Status changes to "Approved"
```

### 6. Verify Approval
```
GET /api/leaves/my (as john_emp)
Expected: See leave with status "Approved" and approvedDate populated
```

---

## ⚠️ COMMON ISSUES & SOLUTIONS

| Issue | Solution |
|-------|----------|
| "Namespace not found" | Check using statements in class files |
| "Migration failed" | Ensure SQL Server is running and connected |
| "401 Unauthorized" | Make sure you're using valid JWT token |
| "403 Forbidden" | Check that Manager role is trying to approve (not Employee) |
| "Overlapping leave error" | Test with non-overlapping dates |
| "Cannot approve own leave" | Use different users (employee and manager) |

---

## 📝 PROMPTS ORDER

1. Create LeaveStatus enum
2. Update LeaveRequest model
3. Create LeaveApprovalDto
4. Create LeaveService
5. Update Program.cs
6. Update ApplicationDbContext
7. Refactor LeavesController
8. Create ApplyLeaveRequest DTO
9. Run migration
10. Build and test

**Total time to implement: ~30-45 minutes**

---

## 🎓 ARCHITECTURE PATTERNS USED

✅ **Dependency Injection** - LeaveService injected into controller
✅ **Service Layer** - Business logic separated from HTTP layer
✅ **DTOs** - Data transfer objects for request/response
✅ **Authorization** - Role-based access control
✅ **Validation** - Business rule validation in service layer
✅ **Error Handling** - Proper HTTP status codes and exception handling

---

## 🔄 Data Flow Example

```
HTTP Request: PUT /api/leaves/1/approve
    ↓
LeavesController.ApproveLeave(1)
    ↓
GetCurrentUserId() → extracts from JWT
    ↓
LeaveService.ApproveLeaveAsync(1, managerId)
    ↓
Validation checks:
  - Leave exists?
  - Already processed?
  - Not own leave?
    ↓
Update Status = Approved
Set ApprovedBy = managerId
Set ApprovedDate = UtcNow
    ↓
_context.SaveChangesAsync()
    ↓
MapToLeaveDetailDto()
    ↓
Return LeaveApprovalResponse (200 OK)
    ↓
HTTP Response with updated leave data
```

---

## ✨ WHAT'S NEXT (FUTURE PHASES)

After Phase 4 is complete, consider:

**Phase 5: Notifications**
- Email notifications when leave is approved/rejected
- Dashboard notifications

**Phase 6: Reports**
- Team leave calendar
- Approval statistics
- Leave balance tracking

**Phase 7: Advanced Features**
- Leave types (Sick, Casual, Earned)
- Leave balance/entitlement
- Delegation of approval authority

---

## 📞 KEY CONTACTS FOR TESTING

When testing, use:
- **Employee User**: username=john_emp, email=john@company.com
- **Manager User**: username=sarah_mgr, email=sarah@company.com

---

## 🚀 START NOW

Open VS Code and paste the prompts from **PHASE4_COPY_PASTE_PROMPTS.md** one by one into the Copilot chat.

Each prompt is self-contained and ready to paste!

