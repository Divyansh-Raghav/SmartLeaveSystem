# Phase 4: Leave Approval Workflow - Code Reference

## 📌 QUICK REFERENCE FOR ALL CREATED FILES

---

## 1. Models/LeaveStatus.cs

```csharp
namespace SmartLeaveManagement.Models;

public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
```

---

## 2. Models/LeaveRequest.cs (Updated Fields)

The following fields were added to the existing LeaveRequest class:

```csharp
// NEW FIELDS FOR APPROVAL WORKFLOW
public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
public int? ApprovedBy { get; set; }
public User? ApprovedByUser { get; set; }
public DateTime? ApprovedDate { get; set; }
public string? RejectionReason { get; set; }
public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
public DateTime? UpdatedDate { get; set; }
```

---

## 3. DTOs/LeaveDto.cs

```csharp
namespace SmartLeaveManagement.DTOs;

// Request DTO for applying leave
public class LeaveApplyRequestDto
{
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

// Request DTO for rejecting leave
public class LeaveRejectRequestDto
{
    public string RejectionReason { get; set; } = string.Empty;
}

// Response DTO for leave details
public class LeaveResponseDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public EmployeeDto? Employee { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    public UserSimpleDto? ApprovedByUser { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

// Simple DTO for employee info in leave responses
public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Simple DTO for user info (approver)
public class UserSimpleDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Generic response wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}
```

---

## 4. Services/LeaveService.cs (Key Methods)

### ApplyLeaveAsync
```csharp
public async Task<LeaveResponseDto> ApplyLeaveAsync(LeaveApplyRequestDto request, int currentUserId)
{
    // Validate date range
    if (request.StartDate >= request.EndDate)
    {
        throw new InvalidOperationException("Start date must be before end date");
    }

    // Check for overlapping leaves
    var overlappingLeave = await _context.LeaveRequests
        .Where(lr => lr.EmployeeId == request.EmployeeId
            && lr.Status != LeaveStatus.Rejected
            && !(lr.EndDate <= request.StartDate || lr.StartDate >= request.EndDate))
        .FirstOrDefaultAsync();

    if (overlappingLeave != null)
    {
        throw new InvalidOperationException("Leave request overlaps with an existing leave");
    }

    // Create and save
    var leaveRequest = new LeaveRequest
    {
        EmployeeId = request.EmployeeId,
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        Reason = request.Reason,
        Status = LeaveStatus.Pending,
        CreatedDate = DateTime.UtcNow
    };

    _context.LeaveRequests.Add(leaveRequest);
    await _context.SaveChangesAsync();

    return MapToResponseDto(leaveRequest);
}
```

### ApproveLeaveAsync
```csharp
public async Task<LeaveResponseDto> ApproveLeaveAsync(int leaveId, int managerId)
{
    var leaveRequest = await _context.LeaveRequests
        .Include(lr => lr.Employee)
        .Include(lr => lr.ApprovedByUser)
        .FirstOrDefaultAsync(lr => lr.Id == leaveId);

    if (leaveRequest == null)
    {
        throw new KeyNotFoundException("Leave request not found");
    }

    // Prevent self-approval
    if (leaveRequest.EmployeeId == managerId)
    {
        throw new UnauthorizedAccessException("You cannot approve your own leave");
    }

    if (leaveRequest.Status != LeaveStatus.Pending)
    {
        throw new InvalidOperationException($"Cannot approve leave that is already {leaveRequest.Status}");
    }

    leaveRequest.Status = LeaveStatus.Approved;
    leaveRequest.ApprovedBy = managerId;
    leaveRequest.ApprovedDate = DateTime.UtcNow;
    leaveRequest.UpdatedDate = DateTime.UtcNow;

    _context.LeaveRequests.Update(leaveRequest);
    await _context.SaveChangesAsync();

    await _context.Entry(leaveRequest).ReloadAsync();
    leaveRequest = await _context.LeaveRequests
        .Include(lr => lr.Employee)
        .Include(lr => lr.ApprovedByUser)
        .FirstAsync(lr => lr.Id == leaveId);

    return MapToResponseDto(leaveRequest);
}
```

### RejectLeaveAsync
```csharp
public async Task<LeaveResponseDto> RejectLeaveAsync(int leaveId, LeaveRejectRequestDto request, int managerId)
{
    var leaveRequest = await _context.LeaveRequests
        .Include(lr => lr.Employee)
        .Include(lr => lr.ApprovedByUser)
        .FirstOrDefaultAsync(lr => lr.Id == leaveId);

    if (leaveRequest == null)
    {
        throw new KeyNotFoundException("Leave request not found");
    }

    if (leaveRequest.EmployeeId == managerId)
    {
        throw new UnauthorizedAccessException("You cannot reject your own leave");
    }

    if (leaveRequest.Status != LeaveStatus.Pending)
    {
        throw new InvalidOperationException($"Cannot reject leave that is already {leaveRequest.Status}");
    }

    if (string.IsNullOrWhiteSpace(request.RejectionReason))
    {
        throw new InvalidOperationException("Rejection reason is required");
    }

    leaveRequest.Status = LeaveStatus.Rejected;
    leaveRequest.ApprovedBy = managerId;
    leaveRequest.ApprovedDate = DateTime.UtcNow;
    leaveRequest.RejectionReason = request.RejectionReason;
    leaveRequest.UpdatedDate = DateTime.UtcNow;

    _context.LeaveRequests.Update(leaveRequest);
    await _context.SaveChangesAsync();

    await _context.Entry(leaveRequest).ReloadAsync();
    leaveRequest = await _context.LeaveRequests
        .Include(lr => lr.Employee)
        .Include(lr => lr.ApprovedByUser)
        .FirstAsync(lr => lr.Id == leaveId);

    return MapToResponseDto(leaveRequest);
}
```

---

## 5. Controllers/LeavesController.cs (Key Endpoints)

### Apply Leave Endpoint
```csharp
[HttpPost("apply")]
public async Task<ActionResult<ApiResponse<LeaveResponseDto>>> ApplyLeave([FromBody] LeaveApplyRequestDto request)
{
    try
    {
        if (request == null)
        {
            return BadRequest(new ApiResponse<LeaveResponseDto>
            {
                Success = false,
                Message = "Request body is required"
            });
        }

        var leaveResponse = await _leaveService.ApplyLeaveAsync(request, GetCurrentUserId());

        return CreatedAtAction(nameof(GetLeaveById), new { id = leaveResponse.Id }, new ApiResponse<LeaveResponseDto>
        {
            Success = true,
            Message = "Leave applied successfully",
            Data = leaveResponse
        });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new ApiResponse<LeaveResponseDto>
        {
            Success = false,
            Message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new ApiResponse<LeaveResponseDto>
        {
            Success = false,
            Message = ex.Message
        });
    }
}
```

### Get Pending Leaves (Manager Only)
```csharp
[HttpGet("pending")]
[Authorize(Roles = "Manager")]
public async Task<ActionResult<ApiResponse<List<LeaveResponseDto>>>> GetPendingLeaves()
{
    try
    {
        var pendingLeaves = await _leaveService.GetPendingLeavesAsync();

        return Ok(new ApiResponse<List<LeaveResponseDto>>
        {
            Success = true,
            Message = $"Retrieved {pendingLeaves.Count} pending leave(s)",
            Data = pendingLeaves
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new ApiResponse<List<LeaveResponseDto>>
        {
            Success = false,
            Message = ex.Message
        });
    }
}
```

### Approve Leave (Manager Only)
```csharp
[HttpPut("{id}/approve")]
[Authorize(Roles = "Manager")]
public async Task<ActionResult<ApiResponse<LeaveResponseDto>>> ApproveLeave(int id)
{
    try
    {
        var managerId = GetCurrentUserId();
        var leaveResponse = await _leaveService.ApproveLeaveAsync(id, managerId);

        return Ok(new ApiResponse<LeaveResponseDto>
        {
            Success = true,
            Message = "Leave approved successfully",
            Data = leaveResponse
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new ApiResponse<LeaveResponseDto>
        {
            Success = false,
            Message = ex.Message
        });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Forbid();
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new ApiResponse<LeaveResponseDto>
        {
            Success = false,
            Message = ex.Message
        });
    }
}
```

### Reject Leave (Manager Only)
```csharp
[HttpPut("{id}/reject")]
[Authorize(Roles = "Manager")]
public async Task<ActionResult<ApiResponse<LeaveResponseDto>>> RejectLeave(int id, [FromBody] LeaveRejectRequestDto request)
{
    try
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return BadRequest(new ApiResponse<LeaveResponseDto>
            {
                Success = false,
                Message = "Rejection reason is required"
            });
        }

        var managerId = GetCurrentUserId();
        var leaveResponse = await _leaveService.RejectLeaveAsync(id, request, managerId);

        return Ok(new ApiResponse<LeaveResponseDto>
        {
            Success = true,
            Message = "Leave rejected successfully",
            Data = leaveResponse
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new ApiResponse<LeaveResponseDto>
        {
            Success = false,
            Message = ex.Message
        });
    }
    catch (UnauthorizedAccessException ex)
    {
        return Forbid();
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new ApiResponse<LeaveResponseDto>
        {
            Success = false,
            Message = ex.Message
        });
    }
}
```

### Get Current User Helper
```csharp
private int GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (int.TryParse(userIdClaim, out var userId))
    {
        return userId;
    }
    throw new UnauthorizedAccessException("Invalid token");
}
```

---

## 6. Data/ApplicationDbContext.cs (Updated)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configure LeaveRequest relationships and indexes
    modelBuilder.Entity<LeaveRequest>()
        .HasOne(lr => lr.Employee)
        .WithMany()
        .HasForeignKey(lr => lr.EmployeeId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<LeaveRequest>()
        .HasOne(lr => lr.ApprovedByUser)
        .WithMany()
        .HasForeignKey(lr => lr.ApprovedBy)
        .OnDelete(DeleteBehavior.SetNull);

    // Create index for performance on frequently queried columns
    modelBuilder.Entity<LeaveRequest>()
        .HasIndex(lr => new { lr.EmployeeId, lr.Status })
        .HasName("IX_LeaveRequests_EmployeeId_Status");

    modelBuilder.Entity<LeaveRequest>()
        .HasIndex(lr => lr.Status)
        .HasName("IX_LeaveRequests_Status");
}
```

---

## 7. Program.cs (Updated)

```csharp
// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
```

---

## 8. Migration SQL (20250125120000_AddLeaveApprovalWorkflow.cs)

```sql
-- Add columns
ALTER TABLE LeaveRequests ADD 
  Status INT NOT NULL DEFAULT 0,
  ApprovedBy INT NULL,
  ApprovedDate DATETIME2 NULL,
  RejectionReason NVARCHAR(MAX) NULL,
  CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  UpdatedDate DATETIME2 NULL;

-- Create indexes
CREATE INDEX IX_LeaveRequests_EmployeeId_Status 
  ON LeaveRequests(EmployeeId, Status);

CREATE INDEX IX_LeaveRequests_Status 
  ON LeaveRequests(Status);

-- Add foreign key
ALTER TABLE LeaveRequests ADD CONSTRAINT
  FK_LeaveRequests_Users_ApprovedBy FOREIGN KEY (ApprovedBy) REFERENCES Users(Id)
  ON DELETE SET NULL;
```

---

## 🔗 VALIDATION LOGIC SNIPPETS

### Check Overlapping Dates
```csharp
var overlappingLeave = await _context.LeaveRequests
    .Where(lr => lr.EmployeeId == employeeId
        && lr.Status != LeaveStatus.Rejected
        && !(lr.EndDate <= startDate || lr.StartDate >= endDate))
    .FirstOrDefaultAsync();

if (overlappingLeave != null)
{
    throw new InvalidOperationException("Leave request overlaps with an existing leave");
}
```

### Validate Date Range
```csharp
if (request.StartDate >= request.EndDate)
{
    throw new InvalidOperationException("Start date must be before end date");
}
```

### Prevent Self-Approval
```csharp
if (leaveRequest.EmployeeId == managerId)
{
    throw new UnauthorizedAccessException("You cannot approve your own leave");
}
```

### Map to DTO
```csharp
private LeaveResponseDto MapToResponseDto(LeaveRequest leaveRequest)
{
    return new LeaveResponseDto
    {
        Id = leaveRequest.Id,
        EmployeeId = leaveRequest.EmployeeId,
        Employee = leaveRequest.Employee != null ? new EmployeeDto
        {
            Id = leaveRequest.Employee.Id,
            Name = leaveRequest.Employee.Name,
            Email = leaveRequest.Employee.Email
        } : null,
        StartDate = leaveRequest.StartDate,
        EndDate = leaveRequest.EndDate,
        Reason = leaveRequest.Reason,
        Status = leaveRequest.Status.ToString(),
        ApprovedBy = leaveRequest.ApprovedBy,
        ApprovedByUser = leaveRequest.ApprovedByUser != null ? new UserSimpleDto
        {
            Id = leaveRequest.ApprovedByUser.Id,
            Username = leaveRequest.ApprovedByUser.Username,
            Email = leaveRequest.ApprovedByUser.Email
        } : null,
        ApprovedDate = leaveRequest.ApprovedDate,
        RejectionReason = leaveRequest.RejectionReason,
        CreatedDate = leaveRequest.CreatedDate,
        UpdatedDate = leaveRequest.UpdatedDate
    };
}
```

---

## 🚀 DEPLOYMENT CHECKLIST

- [ ] Review all created files
- [ ] Run database migration: `dotnet ef database update`
- [ ] Test all endpoints with provided testing guide
- [ ] Verify JWT authentication is working
- [ ] Verify role-based authorization (Employee vs Manager)
- [ ] Test error scenarios
- [ ] Check database indexes are created
- [ ] Verify CreatedDate and UpdatedDate timestamps
- [ ] Test overlapping date validation
- [ ] Test self-approval prevention
- [ ] Deploy to staging environment
- [ ] Run integration tests
- [ ] Deploy to production

