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
