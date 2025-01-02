using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCrypt.Net;
using SeNiko.Entities.Auth.v1;
using SeNiko.Models.Auth.v1;

namespace SeNiko.Controllers.Auth.v1;

[AllowAnonymous]
[ApiVersion("1")]
[Route("seniko/api/v{version:apiVersion}/[controller]")]
[ApiController]
[EnableRateLimiting("FixedWindowThrottlingPolicy")]
[SwaggerTag(description:"User authentication endpoint")]
public sealed class AuthController(IDocumentStore store, IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "User login",
        Description = "Authenticates a user and returns a JWT token.",
        OperationId = "906262fa-732d-4728-af5c-4283bf028dcf")]
    public async Task<ActionResult<string>> Login(
        [FromBody, SwaggerParameter(Description = "Login Request", Required = true)] LoginRequest request, 
        CancellationToken cancellationToken)
    {
        await using var session = store.QuerySession();
        var user = session
            .Query<UserEntity>()
            .FirstOrDefault(x => x.Email == request.Email);
        
        if ((user == null) || (!BCrypt.Net.BCrypt.Verify(user.Password, request.Password)))
            return Unauthorized();

        return Ok(CreateToken(user.Id));
    }

    [AllowAnonymous]
    [HttpPost("register")]    
    [SwaggerOperation(
        Summary = "User registration",
        Description = "Registers a new user and returns the user details.",
        OperationId = "1a2b3c4d-5678-9101-1121-314151617181")]
    public async Task<UserEntity> Signin(
        [FromBody, SwaggerParameter(Description = "Register Request", Required = true)] CreateUserEntity request,
        CancellationToken cancellationToken)
    {
        var newRegistration = new UserEntity(
            request.UserName,
            BCrypt.Net.BCrypt.HashPassword(request.Password, SaltRevision.Revision2X),
            request.Email);
        
        await using var session = store.LightweightSession();
        session.Store(newRegistration);
        await session.SaveChangesAsync(cancellationToken);
        
        return await Task.FromResult(newRegistration);
    }

    private String CreateToken(Guid userId)
    {
        Claim[] claims =
        [
            new("userId", userId.ToString())
        ];

        SymmetricSecurityKey securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration.GetSection("JwtSettings:SecretKey").Value ?? throw new InvalidOperationException(message:"JWT Tokenkey not found"))
        );
        
        SigningCredentials credentials = new SigningCredentials(
                securityKey, 
                SecurityAlgorithms.HmacSha512
            );

        SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credentials,
            Expires = DateTime.UtcNow.AddDays(1),
        };
        
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}
