using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly string BaseUrl =
        "https://storage.c2.liara.space/mechanic-umz";

    [NonAction]
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }
}