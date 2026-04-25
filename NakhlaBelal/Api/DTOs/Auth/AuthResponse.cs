namespace NakhlaBelal.Api.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime Expiry { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public IList<string> Roles { get; set; } = new List<string>();
}
