using Microsoft.AspNetCore.Mvc;
using ManaFood.AuthLambda.Models;
using ManaFood.AuthLambda.Services;

namespace ManaFood.AuthLambda.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Cpf) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "CPF and Password are required" });

        var result = await _authService.AuthenticateAsync(request.Cpf, request.Password);
        
        return result != null 
            ? Ok(result) 
            : Unauthorized(new { message = "Invalid credentials" });
    }
}