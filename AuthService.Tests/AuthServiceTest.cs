namespace AuthService.Tests;

using AuthService.Services;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using AuthService.Services;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class AuthServiceTests
{
    private readonly AuthDbContext _context;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: "AuthServiceTestDb")
            .Options;

        _context = new AuthDbContext(options);

        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["JwtSettings:Secret"]).Returns("TestSecrdfgdfgdfgd32453245adfgadfgasdrfgdfgasdfgadfgdgetKey1234567890");
        _mockConfig.Setup(c => c["JwtSettings:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["JwtSettings:Audience"]).Returns("TestAudience");
        _mockConfig.Setup(c => c["JwtSettings:ExpirationMinutes"]).Returns("60");

        _mockLogger = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(_context, _mockConfig.Object, _mockLogger.Object);
    }
    
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
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
        Dispose();
    }
    
    
    [Fact]
    public async Task RegisterAsync_ShouldThrowException_ForDuplicateEmail()
    {
        var registerDtoNewUser = new RegisterDto
        {
            Username = "TestUser",
            Email = "test@example.com",
            Password = "Test@1234",
            Role = "Admin"
        };
        
        var registerDtoExistingUser = new RegisterDto
        {
            Username = "DuplicateUser",
            Email = "test@example.com",
            Password = "Test@1234",
            Role = "Admin"
        };

        await _authService.RegisterAsync(registerDtoNewUser);
        
        

        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.RegisterAsync(registerDtoExistingUser));
        Assert.Equal("User with this email already exists.", exception.Message);
        Dispose();

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
        Dispose();

    }
    
    [Fact]
    public async Task LoginAsync_ShouldThrowException_ForInvalidEmail()
    {
        var loginDto = new LoginDto
        {
            Email = "nonexistent@example.com",
            Password = "Test@1234"
        };

        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("Invalid email or password.", exception.Message);
        Dispose();

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

        var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginDto));
        Assert.Equal("Invalid email or password.", exception.Message);
        Dispose();

    }
    
   




}
