using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartLeaveManagement.DTOs;
using SmartLeaveManagement.Services;

namespace SmartLeaveManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protect all endpoints by default
public class LeavesController : ControllerBase
{
    private readonly ILeaveService _leaveService;

    public LeavesController(ILeaveService leaveService)
    {
        _leaveService = leaveService;
    }

    /// <summary>
    /// Apply for leave
    /// POST /api/leaves/apply
    /// </summary>
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

    /// <summary>
    /// Get my leaves (employee's own leaves)
    /// GET /api/leaves/my
    /// GET /api/leaves/my-leaves
    /// </summary>
    [HttpGet("my")]
    [HttpGet("my-leaves")]
    public async Task<ActionResult<ApiResponse<List<LeaveResponseDto>>>> GetMyLeaves()
    {
        try
        {
            var employeeId = GetCurrentUserId();
            var leaves = await _leaveService.GetMyLeavesAsync(employeeId);

            return Ok(new ApiResponse<List<LeaveResponseDto>>
            {
                Success = true,
                Message = $"Retrieved {leaves.Count} leave(s)",
                Data = leaves
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

    /// <summary>
    /// Get all pending leaves (Manager only)
    /// GET /api/leaves/pending
    /// </summary>
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

    /// <summary>
    /// Approve a leave request (Manager only)
    /// PUT /api/leaves/{id}/approve
    /// </summary>
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LeaveResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Reject a leave request (Manager only)
    /// PUT /api/leaves/{id}/reject
    /// </summary>
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
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<LeaveResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Get a specific leave by ID
    /// GET /api/leaves/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<LeaveResponseDto>> GetLeaveById(int id)
    {
        // Placeholder for getting a specific leave detail
        return NotFound();
    }

    /// <summary>
    /// Helper method to extract current user ID from JWT token
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Invalid token");
    }
}
