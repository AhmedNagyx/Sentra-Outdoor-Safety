using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentra.API.Data;
using Sentra.API.Models;
using Sentra.API.Models.DTOs;
using Sentra.API.Services;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SentraDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IConfiguration _config;

    public AuthController(SentraDbContext db, IJwtService jwt, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _config = config;
    }

    // =========================
    // REGISTER
    // =========================
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
            return BadRequest(new { message = "Email already exists" });

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHashed = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRoles.Resident
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "User created successfully" });
    }

    // =========================
    // LOGIN
    // =========================
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        // Always run BCrypt to prevent timing attacks
        var dummyHash = "$2a$11$invalidhashtopreventtimingattacksXXXXXXXXXXXXXXXXXXX";
        var hashToVerify = user?.PasswordHashed ?? dummyHash;
        var isValid = BCrypt.Net.BCrypt.Verify(dto.Password, hashToVerify);

        if (user == null || !isValid)
            return Unauthorized(new { message = "Invalid email or password" });

        // Update login metadata
        user.LastLoginAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(dto.FCMToken))
            user.FCMToken = dto.FCMToken;

        // Generate tokens
        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshTokenString = _jwt.GenerateRefreshToken();

        var days = int.TryParse(
            _config["JwtSettings:RefreshTokenExpirationDays"], out var d) ? d : 7;

        var refreshToken = new RefreshToken
        {
            Token = refreshTokenString,
            UserId = user.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshTokenString,
            user.UserId,
            user.Name,
            user.Email,
            user.Role
        });
    }

    // =========================
    // REFRESH TOKEN
    // =========================
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { message = "Invalid or expired refresh token" });

        // Revoke old token
        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;

        // Issue new tokens
        var newAccessToken = _jwt.GenerateAccessToken(stored.User);
        var newRefreshTokenString = _jwt.GenerateRefreshToken();

        var days = int.TryParse(
            _config["JwtSettings:RefreshTokenExpirationDays"], out var d) ? d : 7;

        var newRefreshToken = new RefreshToken
        {
            Token = newRefreshTokenString,
            UserId = stored.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(days)
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshTokenString
        });
    }
}