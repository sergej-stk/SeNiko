namespace SeNiko.Entities.Auth.v1;

public class RegisterEntity(string username, string password, string email)
{
    [SwaggerSchema(Description = "Unique identifier of the user.")]
    public Guid Id { get; set; }

    [SwaggerSchema(Description = "Username chosen by the user for registration.")]
    public string Username { get; set; } = username;

    [JsonIgnore]
    [SwaggerSchema(Description = "Password chosen by the user. This field is ignored in JSON responses.", ReadOnly = true)]
    public string Password { get; set; } = password;

    [EmailAddress]
    [SwaggerSchema(Description = "Email address of the user. Must be a valid email format.")]
    public string Email { get; set; } = email;
}