namespace SmartLeaveManagement.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartLeaveManagement.Data;
using SmartLeaveManagement.DTOs;
using SmartLeaveManagement.Models;
using SmartLeaveManagement.Services;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthController(ApplicationDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new AuthResponse 
            { 
                Success = false, 
                Message = "Email already exists" 
            });
        }

        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            return BadRequest(new AuthResponse 
            { 
                Success = false, 
                Message = "Username already exists" 
            });
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = request.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Registration successful",
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            return Unauthorized(new AuthResponse 
            { 
                Success = false, 
                Message = "Invalid credentials" 
            });
        }

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role
            }
        });
    }

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(20);

            var hashWithSalt = new byte[36];
            Array.Copy(salt, 0, hashWithSalt, 0, 16);
            Array.Copy(hash, 0, hashWithSalt, 16, 20);

            return Convert.ToBase64String(hashWithSalt);
        }
    }

    private bool VerifyPassword(string password, string hash)
    {
        var hashWithSalt = Convert.FromBase64String(hash);
        var salt = new byte[16];
        Array.Copy(hashWithSalt, 0, salt, 0, 16);

        var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
        var computedHash = pbkdf2.GetBytes(20);

        for (int i = 0; i < 20; i++)
        {
            if (hashWithSalt[i + 16] != computedHash[i])
            {
                return false;
            }
        }

        return true;
    }
}
