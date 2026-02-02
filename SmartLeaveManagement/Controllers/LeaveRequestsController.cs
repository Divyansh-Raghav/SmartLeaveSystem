using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLeaveManagement.Data;
using SmartLeaveManagement.Models;

namespace SmartLeaveManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protect all endpoints by default
public class LeaveRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LeaveRequestsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/LeaveRequests
    [HttpGet]
    [Authorize(Roles = "Manager")] // Only managers can see all leave requests
    public async Task<ActionResult<IEnumerable<LeaveRequest>>> GetAll()
    {
        return await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .ToListAsync();
    }

    // GET: api/LeaveRequests/5
    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveRequest>> GetLeaveRequest(int id)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (leaveRequest == null)
        {
            return NotFound();
        }

        return leaveRequest;
    }

    // GET: api/LeaveRequests/employee/5
    [HttpGet("employee/{employeeId}")]
    public async Task<ActionResult<IEnumerable<LeaveRequest>>> GetLeaveRequestsByEmployee(int employeeId)
    {
        return await _context.LeaveRequests
            .Include(lr => lr.Employee)
            .Where(lr => lr.EmployeeId == employeeId)
            .ToListAsync();
    }

    // POST: api/LeaveRequests
    [HttpPost]
    public async Task<ActionResult<LeaveRequest>> Create(LeaveRequest request)
    {
        // Ensure Id is 0 so EF treats it as a new entity
        request.Id = 0;  // Forces EF to treat it as a new entity
        
        // Validate that the employee exists
        var employeeExists = await _context.Employees.AnyAsync(e => e.Id == request.EmployeeId);
        if (!employeeExists)
        {
            return BadRequest("Employee does not exist.");
        }

        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLeaveRequest), new { id = request.Id }, request);
    }

    // PUT: api/LeaveRequests/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, LeaveRequest request)
    {
        var existing = await _context.LeaveRequests.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        // Validate employee exists
        var employeeExists = await _context.Employees
            .AnyAsync(e => e.Id == request.EmployeeId);

        if (!employeeExists)
        {
            return BadRequest("Employee does not exist.");
        }

        // Update only allowed fields
        existing.EmployeeId = request.EmployeeId;
        existing.StartDate = request.StartDate;
        existing.EndDate = request.EndDate;
        existing.Reason = request.Reason;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(409, "Concurrency conflict occurred.");
        }

        return NoContent();
    }


    // DELETE: api/LeaveRequests/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var request = await _context.LeaveRequests.FindAsync(id);
        if (request == null)
        {
            return NotFound();
        }

        _context.LeaveRequests.Remove(request);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool LeaveRequestExists(int id)
    {
        return _context.LeaveRequests.Any(lr => lr.Id == id);
    }
}
