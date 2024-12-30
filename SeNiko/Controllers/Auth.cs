using Marten;

namespace SeNiko.Controllers;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[EnableRateLimiting("FixedWindowThrottlingPolicy")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    IDocumentStore _store;   
    
    public AuthController(IDocumentStore store)
    {
        _store = store;
    }
    
    [HttpPost("login")]
    public async Task<String> Login([FromBody] LoginRequest request)
    {
        return await Task.FromResult("");
    }

    [HttpPost("register")]    
    public async Task<String> Signin([FromBody] LoginRequest request)
    {
        return await Task.FromResult("");
    }
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    public string  email;
    
    [Required]
    public required string password;

    public LoginRequest(string email, string password)
    {
        this.email = email;
        this.password = password;
    }
}
