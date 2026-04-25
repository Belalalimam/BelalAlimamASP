using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NakhlaBelal.Api;
using NakhlaBelal.Api.DTOs.Auth;
using NakhlaBelal.Api.Services;
using NakhlaBelal.Models;
using NakhlaBelal.Utitlies;

namespace NakhlaBelal.Controllers.Api;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwtService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwtService,
        IEmailSender emailSender,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _emailSender = emailSender;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return ApiErrors("Validation failed");

        var user = request.UserNameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(request.UserNameOrEmail)
            : await _userManager.FindByNameAsync(request.UserNameOrEmail);

        if (user == null)
            return ApiFail("Invalid credentials", 401);

        if (!user.EmailConfirmed)
            return ApiFail("Email not confirmed. Please verify your email first.", 403);

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return ApiFail("Invalid credentials", 401);

        var roles = await _userManager.GetRolesAsync(user);
        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60");
        var token = _jwtService.GenerateToken(user, roles);

        return ApiOk(new AuthResponse
        {
            Token = token,
            Expiry = DateTime.UtcNow.AddMinutes(expiryMinutes),
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName ?? "",
            LastName = user.LastName ?? "",
            Roles = roles
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return ApiErrors("Validation failed");

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return ApiFail("Email already registered", 409);

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new NakhlaBelal.Api.Wrappers.ApiResponse<object>
            {
                Success = false,
                Message = "Registration failed",
                Errors = result.Errors.Select(e => e.Description)
            });

        await _userManager.AddToRoleAsync(user, SD.CUSTOMER_ROLE);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var baseUrl = _config["WhatsApp:SiteBaseUrl"] ?? "";
        var confirmUrl = $"{baseUrl}/Identity/Account/ConfirmEmail?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        await _emailSender.SendEmailAsync(user.Email!, "Confirm your email",
            $"<p>Please confirm your account by clicking <a href='{confirmUrl}'>here</a>.</p>");

        return StatusCode(201, NakhlaBelal.Api.Wrappers.ApiResponse<object>.Ok(null,
            "Registration successful. Please check your email to confirm your account."));
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Logout()
    {
        return ApiOk<object>(null!, "Logged out successfully");
    }
}
