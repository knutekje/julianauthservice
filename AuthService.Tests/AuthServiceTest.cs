namespace AuthService.Tests;

using AuthService.Services;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class AuthServiceTests : IAsyncLifetime
{
    private AuthDbContext _context;
    private Mock<IConfiguration> _mockConfig;
    private Mock<ILogger<AuthService>> _mockLogger;
    private AuthService _authService;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuthDbContext(options);

        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["JwtSettings:Secret"])
            .Returns("TestSecretKey1234567890jbljhbkhvbknhb435");
        _mockConfig.Setup(c => c["JwtSettings:Issuer"])
            .Returns("TestIssuer");
        _mockConfig.Setup(c => c["JwtSettings:Audience"])
            .Returns("TestAudience");
        _mockConfig.Setup(c => c["JwtSettings:ExpirationMinutes"])
            .Returns("60");

        _mockLogger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(_context, _mockConfig.Object, _mockLogger.Object);

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task RegisterAsync_ShouldRegisterUserSuccessfully()
    {
        var registerDto = new RegisterDto
        {
            Username = "TestUser",
            Email = "test@example.com",
            Password = "Test@1234",
            Role = "Admin"
        };

        var result = await _authService.RegisterAsync(registerDto);

        Assert.Equal("Registration successful!", result);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        Assert.NotNull(user);
        Assert.Equal(registerDto.Username, user.Username);
        Assert.Equal(registerDto.Email, user.Email);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_ForDuplicateEmail()
    {
        var registerDto = new RegisterDto
        {
            Username = "TestUser",
            Email = "test@example.com",
            Password = "Test@1234",
            Role = "Admin"
        };

        await _authService.RegisterAsync(registerDto);

        var duplicateDto = new RegisterDto
        {
            Username = "OtherUser",
            Email = "test@example.com",
            Password = "Test@1234",
            Role = "User"
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(duplicateDto));
        Assert.Equal("User with this email already exists.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnJwtToken_ForValidCredentials()
    {
        var user = new User
        {
            Username = "TestUser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Test@1234"
        };

        var token = await _authService.LoginAsync(loginDto);

        Assert.NotNull(token);
        Assert.IsType<string>(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_ForInvalidEmail()
    {
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Test@1234"
        };

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("Invalid email or password.", exception.Message);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_ForInvalidPassword()
    {
        var user = new User
        {
            Username = "TestUser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            Role = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("Invalid email or password.", exception.Message);
    }
}
