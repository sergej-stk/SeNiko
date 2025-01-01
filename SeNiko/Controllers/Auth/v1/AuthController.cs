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
    [SwaggerOperation(
        Summary = "User login",
        Description = "Authenticates a user and returns a JWT token.",
        OperationId = "906262fa-732d-4728-af5c-4283bf028dcf")]
    public async Task<LoginRequest> Login(
        [FromBody, SwaggerParameter(Description = "Login Request", Required = true)] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        return await Task.FromResult(request);
    }

    [HttpPost("register")]    
    [SwaggerOperation(
        Summary = "User registration",
        Description = "Registers a new user and returns the user details.",
        OperationId = "1a2b3c4d-5678-9101-1121-314151617181")]
    public async Task<RegisterRequest> Signin(
        [FromBody, SwaggerParameter(Description = "Register Request", Required = true)] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await using var session = _store.LightweightSession();
        session.Store(request);
        await session.SaveChangesAsync(cancellationToken);
        
        return await Task.FromResult(request);
    }
}
