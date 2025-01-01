namespace SeNiko.Controllers.Auth.v1;

[AllowAnonymous]
[ApiVersion("1")]
[Route("seniko/api/v{version:apiVersion}/[controller]")]
[ApiController]
[EnableRateLimiting("FixedWindowThrottlingPolicy")]
[SwaggerTag(description:"User authentication endpoint")]
public sealed class AuthController(IDocumentStore store) : ControllerBase
{
    private IDocumentStore _store = store;

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
