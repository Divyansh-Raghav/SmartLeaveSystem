namespace SmartLeaveManagement.Services;

using Microsoft.EntityFrameworkCore;
using SmartLeaveManagement.Data;
using SmartLeaveManagement.DTOs;
using SmartLeaveManagement.Models;

public interface ILeaveService
{
    Task<LeaveResponseDto> ApplyLeaveAsync(LeaveApplyRequestDto request, int currentUserId);
    Task<List<LeaveResponseDto>> GetMyLeavesAsync(int currentUserId);
    Task<List<LeaveResponseDto>> GetPendingLeavesAsync();
    Task<LeaveResponseDto> ApproveLeaveAsync(int leaveId, int managerId);
    Task<LeaveResponseDto> RejectLeaveAsync(int leaveId, LeaveRejectRequestDto request, int managerId);
}

public class LeaveService : ILeaveService
{
    private readonly ApplicationDbContext _context;

    public LeaveService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Resolve or create the Employee linked to the current user (match by email)
    /// </summary>
    private async Task<Employee?> ResolveEmployeeForUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return null;

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == user.Email);
        if (employee != null) return employee;

        // Auto-provision employee record for existing user
        employee = new Employee
        {
            Name = string.IsNullOrWhiteSpace(user.Username) ? user.Email : user.Username,
            Email = user.Email
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    /// <summary>
    /// Apply for leave with validation
    /// </summary>
    public async Task<LeaveResponseDto> ApplyLeaveAsync(LeaveApplyRequestDto request, int currentUserId)
    {
        // Validate date range
        if (request.StartDate >= request.EndDate)
        {
            throw new InvalidOperationException("Start date must be before end date");
        }

        // Resolve or create employee for current user
        var employee = await ResolveEmployeeForUserAsync(currentUserId);
        if (employee == null)
        {
            throw new InvalidOperationException("Employee does not exist");
        }

        // Check for overlapping leaves (excluding rejected ones)
        var overlappingLeave = await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == employee.Id
                && lr.Status != LeaveStatus.Rejected
                && !(lr.EndDate <= request.StartDate || lr.StartDate >= request.EndDate))
            .FirstOrDefaultAsync();

        if (overlappingLeave != null)
        {
            throw new InvalidOperationException("Leave request overlaps with an existing leave");
        }

        // Create new leave request
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = LeaveStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        // Load navigation for response
        await _context.Entry(leaveRequest).Reference(l => l.Employee).LoadAsync();
        return MapToResponseDto(leaveRequest);
    }

    /// <summary>
    /// Get all leaves for the current user (mapped to employee)
    /// </summary>
    public async Task<List<LeaveResponseDto>> GetMyLeavesAsync(int currentUserId)
    {
        var employee = await ResolveEmployeeForUserAsync(currentUserId);
        if (employee == null)
        {
            return new List<LeaveResponseDto>();
        }

        var leaves = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.ApprovedByUser)
            .Where(lr => lr.EmployeeId == employee.Id)
            .OrderByDescending(lr => lr.CreatedDate)
            .ToListAsync();

        return leaves.Select(MapToResponseDto).ToList();
    }

    /// <summary>
    /// Get all pending leaves (for managers)
    /// </summary>
    public async Task<List<LeaveResponseDto>> GetPendingLeavesAsync()
    {
        var pendingLeaves = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.ApprovedByUser)
            .Where(lr => lr.Status == LeaveStatus.Pending)
            .OrderBy(lr => lr.StartDate)
            .ToListAsync();

        return pendingLeaves.Select(MapToResponseDto).ToList();
    }

    /// <summary>
    /// Approve a leave request (manager only)
    /// </summary>
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

        // Prevent manager from approving their own leave
        if (leaveRequest.EmployeeId == managerId)
        {
            throw new UnauthorizedAccessException("You cannot approve your own leave");
        }

        // Check if already processed
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

        // Reload to get updated navigation properties
        await _context.Entry(leaveRequest).ReloadAsync();
        leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.ApprovedByUser)
            .FirstAsync(lr => lr.Id == leaveId);

        return MapToResponseDto(leaveRequest);
    }

    /// <summary>
    /// Reject a leave request (manager only)
    /// </summary>
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

        // Prevent manager from rejecting their own leave
        if (leaveRequest.EmployeeId == managerId)
        {
            throw new UnauthorizedAccessException("You cannot reject your own leave");
        }

        // Check if already processed
        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot reject leave that is already {leaveRequest.Status}");
        }

        // Validate rejection reason
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

        // Reload to get updated navigation properties
        await _context.Entry(leaveRequest).ReloadAsync();
        leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.ApprovedByUser)
            .FirstAsync(lr => lr.Id == leaveId);

        return MapToResponseDto(leaveRequest);
    }

    // Helper method to map LeaveRequest to LeaveResponseDto
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
}
