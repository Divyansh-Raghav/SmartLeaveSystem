namespace SmartLeaveManagement.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;

    // NEW FIELDS FOR APPROVAL WORKFLOW
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public int? ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}
