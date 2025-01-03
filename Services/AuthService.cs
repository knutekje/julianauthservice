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
        // Check if username already exists
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
            _logger.LogWarning("Registration failed for email {Email}: Email already exists", dto.Email);
            throw new Exception("User with this email already exists.");
        }

        // Check if email domain is allowed (example restriction)
        var allowedDomains = new[] { "example.com", "test.com" };
        var emailDomain = dto.Email.Split('@').Last();
        if (!allowedDomains.Contains(emailDomain))
        {
            _logger.LogWarning("Registration failed: Email domain {Domain} is not allowed.", emailDomain);
            throw new Exception("Email domain is not allowed.");
        }

        // Proceed with user creation
        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Username} registered successfully.", dto.Username);

        return "Registration successful!";
    }


    public async Task<string> LoginAsync(LoginDto dto)
    {
        // Check if the user exists
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found.", dto.Email);
            throw new Exception("Invalid email or password.");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed: Invalid password for email {Email}.", dto.Email);
            throw new Exception("Invalid email or password.");
        }

        // Generate JWT token
        var token = GenerateJwt(user);
        _logger.LogInformation("User {Username} logged in successfully.", user.Username);

        return token;
    }
    private string GenerateJwt(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["JwtSettings:ExpirationMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    
    public bool IsPasswordStrong(string password)
    {
        return password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => "!@#$%^&*()".Contains(ch));
    }

}