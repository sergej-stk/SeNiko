namespace SeNiko.Models.Auth.v1;

public class CreateUserEntity
{
    [Required]
    [SwaggerSchema(Description = "Username chosen by the user for registration.")]
    public string UserName { get; set; }

    [Required]
    [MinLength(5)]
    [SwaggerSchema(Description = "Password chosen by the user.")]
    public string Password { get; set; }

    [Required]
    [EmailAddress]
    [SwaggerSchema(Description = "Email address of the user. Must be a valid email format.")]
    public string Email { get; set; }    
}