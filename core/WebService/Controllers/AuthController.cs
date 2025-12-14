using Microsoft.AspNetCore.Mvc;
using SharedContracts.Auth;
using WebService.Services.Auth;

namespace WebService.Controllers;

[ApiController]
[Route("api/[controller]")]
[EndpointGroupName("public")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(request);
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized();
        }

        var userInfo = await _authService.GetUserInfoAsync(userId);
        if (userInfo == null)
        {
            return NotFound();
        }

        return Ok(userInfo);
    }
}