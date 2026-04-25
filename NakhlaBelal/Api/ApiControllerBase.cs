using Microsoft.AspNetCore.Mvc;
using NakhlaBelal.Api.Wrappers;

namespace NakhlaBelal.Api;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ApiOk<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult ApiFail(string message, int statusCode = 400) =>
        StatusCode(statusCode, ApiResponse<object>.Fail(message));

    protected IActionResult ApiErrors(string message) =>
        BadRequest(ApiResponse<object>.Fail(message,
            ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
}
