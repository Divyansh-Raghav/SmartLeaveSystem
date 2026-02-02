# 🎯 PHASE 4: LEAVE APPROVAL WORKFLOW - COPY-PASTE PROMPTS

Copy-paste these prompts **one at a time** into VS Code Copilot.

---

## **PROMPT 1: Create LeaveStatus Enum**

```
Create a new file: SmartLeaveManagement/Models/LeaveStatus.cs

This file should contain a public enum called LeaveStatus with these values:
- Pending = 0
- Approved = 1
- Rejected = 2

Use C# 12 file-scoped namespace syntax.
```

---

## **PROMPT 2: Update LeaveRequest Model**

```
Open file: SmartLeaveManagement/Models/LeaveRequest.cs

Add these new properties to the LeaveRequest class:
1. Status property of type LeaveStatus (default value: LeaveStatus.Pending)
2. ApprovedBy property of type int? (nullable)
3. ApprovedByUser property of type User? (nullable navigation property)
4. ApprovedDate property of type DateTime? (nullable)
5. RejectionReason property of type string with empty string default
6. CreatedDate property of type DateTime with default DateTime.UtcNow
7. UpdatedDate property of type DateTime? (nullable)

Keep all existing properties as they are.
Use C# 12 file-scoped namespace syntax.
```

---

## **PROMPT 3: Create Leave DTOs**

```
Create a new file: SmartLeaveManagement/DTOs/LeaveApprovalDto.cs

This file should contain these C# classes:

1. ApproveLeaveRequest
   - empty class (just a marker for now)

2. RejectLeaveRequest
   - RejectionReason: string property with empty string default

3. LeaveDetailDto
   - Id: int
   - EmployeeId: int
   - EmployeeName: string with empty string default
   - EmployeeEmail: string with empty string default
   - StartDate: DateTime
   - EndDate: DateTime
   - Reason: string with empty string default
   - Status: string with empty string default
   - ApprovedBy: int?
   - ApprovedByUserName: string? with null default
   - ApprovedDate: DateTime?
   - RejectionReason: string with empty string default
   - CreatedDate: DateTime
   - UpdatedDate: DateTime?

4. LeaveApprovalResponse
   - Success: bool
   - Message: string with empty string default
   - Data: LeaveDetailDto?

Use C# 12 file-scoped namespace syntax.
```

---

## **PROMPT 4: Create LeaveService**

```
Create a new file: SmartLeaveManagement/Services/LeaveService.cs

This service should:

1. Be an interface ILeaveService with these methods:
   - Task<LeaveRequest> ApplyForLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, string reason)
   - Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeLeaveId = null)
   - Task<LeaveRequest> ApproveLeaveAsync(int leaveId, int managerId)
   - Task<LeaveRequest> RejectLeaveAsync(int leaveId, int managerId, string rejectionReason)

2. Be a concrete class LeaveService implementing ILeaveService with these features:
   - Constructor takes ApplicationDbContext and ILogger<LeaveService>
   - ApplyForLeaveAsync method that:
     - Validates StartDate < EndDate, throws InvalidOperationException if false
     - Checks for overlapping leaves, throws InvalidOperationException if overlap exists
     - Creates new LeaveRequest with Status=Pending
     - Saves to database and returns the created leave
   - HasOverlappingLeaveAsync method that:
     - Queries LeaveRequests for same employeeId
     - Checks if any leaves overlap with given date range
     - Excludes rejected leaves from overlap check
     - Excludes the leave specified by excludeLeaveId if provided
     - Returns true if overlap found
   - ApproveLeaveAsync method that:
     - Gets the leave by ID
     - Throws exception if leave not found or already processed
     - Throws exception if ApprovedBy equals EmployeeId (cannot approve own leave)
     - Sets Status to Approved
     - Sets ApprovedBy, ApprovedDate, UpdatedDate
     - Saves and returns the updated leave
   - RejectLeaveAsync method that:
     - Gets the leave by ID
     - Throws exception if leave not found or already processed
     - Throws exception if ApprovedBy equals EmployeeId (cannot reject own leave)
     - Sets Status to Rejected
     - Sets ApprovedBy, ApprovedDate, RejectionReason, UpdatedDate
     - Saves and returns the updated leave

Use dependency injection pattern.
Use C# 12 features.
Add XML comments for public methods.
```

---

## **PROMPT 5: Update Program.cs**

```
Open file: SmartLeaveManagement/Program.cs

After the line where TokenService is registered (builder.Services.AddScoped<ITokenService, TokenService>()),
add this line to register the LeaveService:

builder.Services.AddScoped<ILeaveService, LeaveService>();

Make sure to import the Services namespace if not already imported.
```

---

## **PROMPT 6: Update ApplicationDbContext**

```
Open file: SmartLeaveManagement/Data/ApplicationDbContext.cs

Add this using statement at the top if not already present:
using Microsoft.EntityFrameworkCore;

Then in the OnModelCreating method (if it exists, otherwise create it after the DbSets):
Add a configuration for LeaveRequest entity:
- Set the Status property default value to LeaveStatus.Pending
- Set the CreatedDate property default value to DateTime.UtcNow
- Add a foreign key relationship from ApprovedBy to Users table
- Add an index on (EmployeeId, Status) for query performance

Use Entity Configuration fluent API syntax.
```

---

## **PROMPT 7: Refactor LeaveRequestsController**

```
Open file: SmartLeaveManagement/Controllers/LeaveRequestsController.cs

This is a major refactoring. Here's what to do:

1. Rename the controller class from LeaveRequestsController to LeavesController
2. Change the route attribute to [Route("api/[controller]")] with path becoming 'leaves'
3. Add [Authorize] attribute to the entire controller
4. Inject ILeaveService in constructor along with ApplicationDbContext
5. Delete all existing GET/POST/PUT/DELETE methods - we'll replace them

Now implement these new methods:

METHOD 1: ApplyForLeave (POST /api/leaves/apply)
- [HttpPost("apply")]
- Parameter: ApplyLeaveRequest request (create this DTO too with EmployeeId, StartDate, EndDate, Reason)
- Get current user from HttpContext.User
- Call leaveService.ApplyForLeaveAsync()
- Return CreatedAtAction with LeaveDetailDto
- Catch exceptions and return BadRequest with error message

METHOD 2: GetMyLeaves (GET /api/leaves/my)
- [HttpGet("my")]
- Get current user ID from HttpContext.User
- Query LeaveRequests where EmployeeId == currentUserId
- Include Employee data
- Order by CreatedDate descending
- Map to LeaveDetailDto
- Return Ok with list

METHOD 3: GetPendingLeaves (GET /api/leaves/pending)
- [HttpGet("pending")]
- [Authorize(Roles = "Manager")]
- Query all LeaveRequests where Status == LeaveStatus.Pending
- Include Employee data
- Order by CreatedDate ascending
- Map to LeaveDetailDto
- Return Ok with list

METHOD 4: ApproveLeave (PUT /api/leaves/{id}/approve)
- [HttpPut("{id}/approve")]
- [Authorize(Roles = "Manager")]
- Parameter: int id
- Get current user from HttpContext.User
- Call leaveService.ApproveLeaveAsync(id, currentUserId)
- Map to LeaveDetailDto
- Return LeaveApprovalResponse with success message
- Catch exceptions and return appropriate status codes (400, 403, 404)

METHOD 5: RejectLeave (PUT /api/leaves/{id}/reject)
- [HttpPut("{id}/reject")]
- [Authorize(Roles = "Manager")]
- Parameters: int id, RejectLeaveRequest request
- Get current user from HttpContext.User
- Call leaveService.RejectLeaveAsync(id, currentUserId, request.RejectionReason)
- Map to LeaveDetailDto
- Return LeaveApprovalResponse with success message
- Catch exceptions and return appropriate status codes

Add a private helper method MapToLeaveDetailDto that converts LeaveRequest to LeaveDetailDto.

Keep the existing CRUD endpoints commented out for now if needed, but prioritize the new approval workflow endpoints.

Add proper using statements for all DTOs and services.
Use [Authorize] attribute effectively.
Use proper HTTP status codes and error handling.
```

---

## **PROMPT 7b: Create ApplyLeaveRequest DTO (if not in LeaveApprovalDto)**

```
Open file: SmartLeaveManagement/DTOs/LeaveApprovalDto.cs

Add this new class if it's not already there:

public class ApplyLeaveRequest
{
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

---

## **PROMPT 8: Create Database Migration**

```
In PowerShell/Terminal, run these commands:

cd SmartLeaveManagement

dotnet ef migrations add UpdateLeaveRequestWithApprovalFields

This will create a new migration file for the LeaveRequest schema changes.
Then apply it:

dotnet ef database update

This will update your SQL Server database with the new columns.
```

---

## **PROMPT 9: Helper Method - Get Current User ID**

```
Open file: SmartLeaveManagement/Controllers/LeavesController.cs

Add this private helper method to the LeavesController class:

private int GetCurrentUserId()
{
    var userIdClaim = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
    {
        return userId;
    }
    throw new UnauthorizedAccessException("User ID not found in token");
}

Use this helper method in all endpoint methods to get the current authenticated user's ID.
```

---

## **PROMPT 10: Add Validation Attributes to LeaveRequest**

```
Open file: SmartLeaveManagement/Models/LeaveRequest.cs

Add these Data Annotations to relevant properties:

1. On StartDate property:
   [Required]

2. On EndDate property:
   [Required]

3. On Reason property:
   [Required]
   [MinLength(5)]
   [MaxLength(500)]

Add using statement: using System.ComponentModel.DataAnnotations;

This will enable model validation.
```

---

## **VERIFICATION CHECKLIST**

After all prompts are executed:

```bash
# 1. Check that project compiles
cd SmartLeaveManagement
dotnet build

# 2. Verify migration was applied
dotnet ef migrations list

# 3. Start the application
dotnet run

# 4. Test endpoints using Swagger at https://localhost:5001/swagger

# 5. Test flows:
# - Register 2 users (john_emp as Employee, sarah_mgr as Manager)
# - Login as john_emp
# - POST /api/leaves/apply to create a leave request
# - GET /api/leaves/my to see your leaves
# - Login as sarah_mgr
# - GET /api/leaves/pending to see pending leaves
# - PUT /api/leaves/{id}/approve to approve a leave
# - Verify status changed to Approved
```

---

## **EXAMPLE CURL COMMANDS FOR TESTING**

```bash
# 1. Register and Login
POST http://localhost:5000/api/auth/register
{
  "username": "john_emp",
  "email": "john@company.com",
  "password": "Password123!",
  "role": "Employee"
}

POST http://localhost:5000/api/auth/register
{
  "username": "sarah_mgr",
  "email": "sarah@company.com",
  "password": "Password123!",
  "role": "Manager"
}

# 2. Apply for Leave (as john_emp)
POST http://localhost:5000/api/leaves/apply
Authorization: Bearer <john_token>
{
  "employeeId": 1,
  "startDate": "2025-02-01",
  "endDate": "2025-02-05",
  "reason": "Vacation in February"
}

# 3. Get My Leaves (as john_emp)
GET http://localhost:5000/api/leaves/my
Authorization: Bearer <john_token>

# 4. Get Pending Leaves (as sarah_mgr - Manager only)
GET http://localhost:5000/api/leaves/pending
Authorization: Bearer <sarah_token>

# 5. Approve Leave (as sarah_mgr - Manager only)
PUT http://localhost:5000/api/leaves/1/approve
Authorization: Bearer <sarah_token>

# 6. Reject Leave (as sarah_mgr - Manager only)
PUT http://localhost:5000/api/leaves/2/reject
Authorization: Bearer <sarah_token>
{
  "rejectionReason": "Insufficient team coverage that week"
}
```

---

## **SUMMARY OF CHANGES**

### Files Created:
- ✅ `Models/LeaveStatus.cs`
- ✅ `DTOs/LeaveApprovalDto.cs`
- ✅ `Services/LeaveService.cs`

### Files Updated:
- ✅ `Models/LeaveRequest.cs` (added fields)
- ✅ `Data/ApplicationDbContext.cs` (added configurations)
- ✅ `Controllers/LeaveRequestsController.cs` → `LeavesController.cs` (refactored)
- ✅ `Program.cs` (registered LeaveService)

### Database:
- ✅ New migration created and applied

---

## **ARCHITECTURE OVERVIEW**

```
Client (Angular)
    ↓
LeavesController (handles HTTP requests)
    ↓
ILeaveService / LeaveService (business logic)
    ↓
ApplicationDbContext (data access)
    ↓
SQL Server Database
```

Each layer is independent and testable.

