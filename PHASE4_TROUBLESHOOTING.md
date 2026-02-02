# Phase 4: Leave Approval Workflow - Troubleshooting Guide

## 🔧 COMMON ISSUES AND SOLUTIONS

---

## ❌ Issue: "Cannot open table 'LeaveRequests'" after migration

**Cause:** Migration not applied to database

**Solution:**
```bash
# Run the migration
dotnet ef database update

# If that doesn't work, check migration status
dotnet ef migrations list

# If migration is pending, apply it
dotnet ef database update --migration AddLeaveApprovalWorkflow
```

---

## ❌ Issue: 401 Unauthorized on all endpoints

**Cause:** JWT token not provided or invalid

**Solution:**
1. Ensure you're including the Authorization header:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

2. Verify token is not expired (default expiry: 2 hours)

3. Get a new token by logging in:
```bash
POST /api/auth/login
{
  "email": "john@company.com",
  "password": "Employee@123"
}
```

4. Check that JWT secret is configured in `appsettings.json`:
```json
"Jwt": {
  "Key": "your-256-bit-secret-key-minimum",
  "Issuer": "SmartLeaveManagement",
  "Audience": "SmartLeaveManagementUsers",
  "ExpiryHours": 2
}
```

---

## ❌ Issue: 403 Forbidden when trying to access manager endpoints

**Cause:** User doesn't have the "Manager" role

**Solution:**
1. Register a new user with Manager role:
```bash
POST /api/auth/register
{
  "username": "manager_user",
  "email": "manager@company.com",
  "password": "Manager@123",
  "role": "Manager"
}
```

2. Login with that user and use the returned token

3. Verify the token contains the Manager role by decoding it (use jwt.io)

---

## ❌ Issue: "Leave request not found" when trying to approve

**Cause:** Invalid leave ID or leave doesn't exist

**Solution:**
1. Get the correct leave ID:
```bash
GET /api/leaves/my
Authorization: Bearer {employee_token}
```

2. Check the returned `id` field

3. Use that ID in the approve endpoint:
```bash
PUT /api/leaves/{correct_id}/approve
```

---

## ❌ Issue: "You cannot approve your own leave"

**Cause:** Manager is trying to approve a leave they applied for

**Solution:**
1. Have a different manager approve the leave

2. Or, the employee's ID matches the current manager's ID

3. Verify you're using the correct token - switch to a different manager token

---

## ❌ Issue: "Leave request overlaps with an existing leave"

**Cause:** Employee already has a non-rejected leave during those dates

**Solution:**
1. Check existing leaves:
```bash
GET /api/leaves/my
Authorization: Bearer {employee_token}
```

2. Use different dates that don't overlap

3. Or reject the existing leave first (if you're a manager):
```bash
PUT /api/leaves/{existing_id}/reject
{
  "rejectionReason": "Cancelled"
}
```

Then apply new leave:
```bash
POST /api/leaves/apply
{
  "employeeId": 1,
  "startDate": "2025-02-15",
  "endDate": "2025-02-20",
  "reason": "New vacation"
}
```

---

## ❌ Issue: "Start date must be before end date"

**Cause:** StartDate >= EndDate in request

**Solution:**
Ensure startDate is before endDate:
```bash
POST /api/leaves/apply
{
  "employeeId": 1,
  "startDate": "2025-02-15",  // Must be BEFORE
  "endDate": "2025-02-20",    // EndDate
  "reason": "Vacation"
}
```

---

## ❌ Issue: "Rejection reason is required"

**Cause:** Rejecting leave without providing a reason

**Solution:**
Include a rejection reason:
```bash
PUT /api/leaves/{id}/reject
{
  "rejectionReason": "Insufficient staff coverage"
}
```

---

## ❌ Issue: "Cannot approve leave that is already Approved"

**Cause:** Trying to approve a leave that's already been processed

**Solution:**
1. Only approve leaves with status = "Pending"

2. Check the current status:
```bash
GET /api/leaves/pending
Authorization: Bearer {manager_token}
```

3. Only approve leaves where `"status": "Pending"`

---

## ❌ Issue: Build fails with "The type 'LeaveStatus' does not exist"

**Cause:** LeaveStatus enum not found

**Solution:**
1. Verify `Models/LeaveStatus.cs` exists

2. Check it has the correct namespace:
```csharp
namespace SmartLeaveManagement.Models;
```

3. Rebuild the solution:
```bash
dotnet clean
dotnet build
```

---

## ❌ Issue: Build fails with "ILeaveService could not be resolved"

**Cause:** LeaveService not registered in Program.cs

**Solution:**
Ensure this line exists in Program.cs:
```csharp
builder.Services.AddScoped<ILeaveService, LeaveService>();
```

And that the using statement is present:
```csharp
using SmartLeaveManagement.Services;
```

---

## ❌ Issue: "The type 'ApiResponse' does not exist"

**Cause:** LeaveDto.cs not in correct location

**Solution:**
1. Verify file exists at: `SmartLeaveManagement/DTOs/LeaveDto.cs`

2. Check it has the correct namespace:
```csharp
namespace SmartLeaveManagement.DTOs;
```

3. Verify the controller imports it:
```csharp
using SmartLeaveManagement.DTOs;
```

---

## ❌ Issue: User.FindFirst() returns null in controller

**Cause:** JWT token doesn't contain the NameIdentifier claim

**Solution:**
1. Check that TokenService adds the claim:
```csharp
new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
```

2. Verify token is valid by decoding it at jwt.io

3. The token should have a "nameid" or "sub" claim with the user ID

---

## ❌ Issue: Database shows old columns, not new ones

**Cause:** Migration not applied correctly

**Solution:**
```bash
# Check migration history
dotnet ef migrations list

# If AddLeaveApprovalWorkflow is there but not applied:
dotnet ef database update

# If you need to revert and reapply:
dotnet ef database update --previous-migration
dotnet ef database update
```

---

## ⚠️ Issue: Timestamps show wrong date/time

**Cause:** Server time is not in UTC or local timezone issue

**Solution:**
All timestamps should be saved as UTC:
```csharp
CreatedDate = DateTime.UtcNow;  // Use UtcNow, not Now
```

And in database:
```sql
DEFAULT GETUTCDATE()  -- SQL Server UTC time
```

If displaying in UI, convert from UTC to local timezone in Angular.

---

## ⚠️ Issue: Endpoints return 500 Internal Server Error

**Cause:** Unhandled exception in service or database error

**Solution:**
1. Check application logs for detailed error message

2. Enable logging in Program.cs:
```csharp
builder.Services.AddLogging();
```

3. Check the Response object contains the error message

4. Common causes:
   - Database not updated with migration
   - Foreign key constraint violation
   - NULL reference exception in mapping
   - Employee/User doesn't exist in database

---

## 🧪 TESTING TROUBLESHOOTING

### Issue: "Cannot create leave for non-existent employee"

**Cause:** Employee ID doesn't exist in database

**Solution:**
1. Create an Employee first:
```bash
POST /api/employees
{
  "name": "John Doe",
  "email": "john@company.com"
}
```

2. Use the returned Employee ID in leave requests

### Issue: Tokens not working with correct role

**Cause:** User registered without proper role

**Solution:**
Delete and re-register:
```bash
POST /api/auth/register
{
  "username": "manager_user",
  "email": "manager@company.com", 
  "password": "Manager@123",
  "role": "Manager"  // Case-sensitive, must be "Manager" or "Employee"
}
```

---

## 📊 DEBUGGING CHECKLIST

When something doesn't work, check in order:

- [ ] Database migration applied: `dotnet ef migrations list`
- [ ] API running: Can you access swagger docs at /swagger?
- [ ] User exists: Try getting that user in database
- [ ] Token valid: Decode token at jwt.io, check claims
- [ ] Token not expired: Token should be less than 2 hours old
- [ ] Role correct: Check "role" claim in decoded token
- [ ] Headers correct: Include `Authorization: Bearer {token}`
- [ ] Request body correct: Validate JSON format
- [ ] Leave ID exists: Check in database or GET /api/leaves/my
- [ ] Leave status correct: Only approve "Pending" leaves
- [ ] Manager not self-approving: Verify different user IDs

---

## 🔍 USEFUL SQL QUERIES FOR DEBUGGING

```sql
-- Check all leaves with their status
SELECT Id, EmployeeId, Status, StartDate, EndDate, CreatedDate, ApprovedBy, ApprovedDate
FROM LeaveRequests
ORDER BY CreatedDate DESC;

-- Check pending leaves
SELECT * FROM LeaveRequests WHERE Status = 0;

-- Check leaves by employee
SELECT * FROM LeaveRequests WHERE EmployeeId = 1;

-- Check who approved what
SELECT 
  lr.Id, 
  lr.EmployeeId, 
  lr.Status,
  lr.ApprovedBy,
  u.Username as ApprovedByUser,
  lr.ApprovedDate
FROM LeaveRequests lr
LEFT JOIN Users u ON lr.ApprovedBy = u.Id
WHERE lr.ApprovedBy IS NOT NULL;

-- Check for overlapping leaves
SELECT 
  lr1.Id as Leave1, 
  lr2.Id as Leave2,
  lr1.EmployeeId,
  lr1.StartDate,
  lr1.EndDate,
  lr2.StartDate,
  lr2.EndDate
FROM LeaveRequests lr1
JOIN LeaveRequests lr2 ON lr1.EmployeeId = lr2.EmployeeId
  AND lr1.Id < lr2.Id
  AND lr1.Status != 2
  AND lr2.Status != 2
  AND NOT (lr1.EndDate <= lr2.StartDate OR lr1.StartDate >= lr2.EndDate);

-- Check indexes created
SELECT * FROM sys.indexes 
WHERE object_id = OBJECT_ID('LeaveRequests');
```

---

## 📞 GETTING HELP

If you encounter an issue not covered here:

1. **Check the error message carefully** - it usually contains the cause
2. **Review PHASE4_API_TESTING_GUIDE.md** - verify your request format
3. **Check PHASE4_CODE_REFERENCE.md** - verify implementation matches
4. **Review the migration** - ensure all columns were added
5. **Check database schema** - use SQL queries above to verify
6. **Enable detailed logging** - see error details in application logs
7. **Test with Postman** - verify endpoint and request format

---

## ✅ VERIFICATION CHECKLIST

After implementing Phase 4, verify:

- [ ] All 5 files created/updated without errors
- [ ] Build completes successfully
- [ ] Database migration applies without errors
- [ ] Can register Employee user
- [ ] Can register Manager user
- [ ] Can login and get JWT token
- [ ] Can apply leave (creates with Pending status)
- [ ] Can get my leaves (employee sees own)
- [ ] Can get pending leaves (manager sees all)
- [ ] Can approve leave (changes to Approved status)
- [ ] Can reject leave (changes to Rejected status)
- [ ] Manager cannot approve own leave (403 Forbidden)
- [ ] Manager cannot reject own leave (403 Forbidden)
- [ ] Employee cannot access /api/leaves/pending (403 Forbidden)
- [ ] Overlapping dates are prevented (400 Bad Request)
- [ ] Invalid date ranges rejected (400 Bad Request)
- [ ] Rejection requires reason (400 Bad Request if empty)

Once all checks pass, Phase 4 backend is ready for frontend integration!

