using AuthService.DTOs;
using AuthService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using AuthService.Services; 
namespace AuthService.Controller;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<Services.AuthService> _logger;


    public AuthController(IAuthService authService, ILogger<Services.AuthService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RegisterAsync(dto);
            return Ok(new { message = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", dto.Email);
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            var token = await _authService.LoginAsync(dto);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
