using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Api.DTOs.Auth;

public class LoginRequest
{
    [Required]
    public string UserNameOrEmail { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}
