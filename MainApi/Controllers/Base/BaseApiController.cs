namespace MainApi.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly string BaseUrl;

    protected BaseApiController(IConfiguration configuration)
    {
        BaseUrl = configuration["LiaraStorage:BaseUrl"] ?? "https://storage.c2.liara.space/mechanic-umz";
    }

    protected BaseApiController()
    {
        BaseUrl = "https://storage.c2.liara.space/mechanic-umz";
    }

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