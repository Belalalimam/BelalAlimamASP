using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Api.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    public string FirstName { get; set; } = "";

    [Required]
    public string LastName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string UserName { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";
}
