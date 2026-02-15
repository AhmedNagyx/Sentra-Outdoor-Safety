using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Models;
using Sentra.API.Models.DTOs;
using System;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SentraDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(SentraDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    // =========================
    // REGISTER
    // =========================
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Email already exists");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHashed = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = "Resident"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok("User created successfully");
    }

    // =========================
    // LOGIN
    // =========================
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null)
            return Unauthorized("Invalid email or password");

        bool valid = BCrypt.Net.BCrypt.Verify(
            dto.Password, user.PasswordHashed);

        if (!valid)
            return Unauthorized("Invalid email or password");

        // Update login time
        user.LastLoginAt = DateTime.UtcNow;

        // Save FCM token if provided
        if (!string.IsNullOrEmpty(dto.FCMToken))
            user.FCMToken = dto.FCMToken;

        await _db.SaveChangesAsync();

        var token = _jwt.Generate(user);

        return Ok(new
        {
            token,
            user.UserId,
            user.Name,
            user.Email,
            user.Role
        });
    }
}
