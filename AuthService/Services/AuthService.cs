using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Interfaces;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;


namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AuthDbContext context, IConfiguration config, ILogger<AuthService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public async Task<string> RegisterAsync(RegisterDto dto)
{
    if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
    {
        _logger.LogWarning("Registration failed: Username {Username} already exists.", dto.Username);
        throw new Exception("Username already exists.");
    }

    if (!IsPasswordStrong(dto.Password))
    {
        _logger.LogWarning("Registration failed: Password does not meet complexity requirements.");
        throw new Exception("Password must be at least 8 characters long and include uppercase, lowercase, a number, and a special character.");
    }

    if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
    {
        _logger.LogWarning("Registration failed for email {Email}: Email already exists.", dto.Email);
        throw new Exception("User with this email already exists.");
    }

    var allowedDomains = new[] { "example.com", "test.com" };
    var emailDomain = dto.Email.Split('@').Last();
    if (!allowedDomains.Contains(emailDomain))
    {
        _logger.LogWarning("Registration failed: Email domain {Domain} is not allowed.", emailDomain);
        throw new Exception("Email domain is not allowed.");
    }

    var allowedRoles = new[] { Roles.Admin, Roles.Receptionist, Roles.Housekeeping, Roles.FAndB, Roles.Maintenance };
    if (!allowedRoles.Contains(dto.Role))
    {
        _logger.LogWarning("Registration failed: Role {Role} is invalid.", dto.Role);
        throw new Exception("Invalid role specified.");
    }

    var user = new User
    {
        Username = dto.Username,
        Email = dto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        Role = dto.Role
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    _logger.LogInformation("User {Username} registered successfully with role {Role}.", dto.Username, dto.Role);

    return "Registration successful!";
}



    public async Task<string> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            _logger.LogWarning("Login failed: Email or password is missing.");
            throw new ArgumentException("Email and password are required.");
        }

        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found.", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed: Invalid password for email {Email}.", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = GenerateJwt(user);
        _logger.LogInformation("User {Username} logged in successfully.", user.Username);

        return token;
    }

    private string GenerateJwt(User user)
    {
        var secret = _config["JwtSettings:Secret"];
        var issuer = _config["JwtSettings:Issuer"];
        var audience = _config["JwtSettings:Audience"];
        var expirationMinutes = _config["JwtSettings:ExpirationMinutes"];

        _logger.LogDebug("JWT Config - Secret: {Secret}, Issuer: {Issuer}, Audience: {Audience}, Expiration: {Expiration}",
            secret, issuer, audience, expirationMinutes);

        if (string.IsNullOrWhiteSpace(secret))
            throw new Exception("JWT Secret is not configured.");
        if (string.IsNullOrWhiteSpace(issuer))
            throw new Exception("JWT Issuer is not configured.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new Exception("JWT Audience is not configured.");
        if (!int.TryParse(expirationMinutes, out var expiration))
            throw new Exception("JWT ExpirationMinutes is not configured or invalid.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiration),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }


    
    private bool IsPasswordStrong(string password)
    {
        var hasUpperCase = password.Any(char.IsUpper);
        var hasLowerCase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));
        return password.Length >= 8 && hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
    }


}