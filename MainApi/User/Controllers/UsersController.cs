namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : BaseApiController
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int id)
    {
        var currentUserId = CurrentUser.UserId;
        if (currentUserId == null) return Unauthorized();
        if (currentUserId != id && !CurrentUser.IsAdmin) return Forbid();

        var query = new GetAdminUserByIdQuery(id); // Using Admin query for detail if own user
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto updateRequest)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new UpdateUserCommand(id, updateRequest, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);

        // لاگین با OTP شروع می‌شود
        var command = new RequestOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            IpAddress = clientIp
        };
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new VerifyOtpCommand
        {
            PhoneNumber = request.PhoneNumber,
            Code = request.Code,
            IpAddress = clientIp,
            UserAgent = userAgent
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequestDto request)
    {
        var clientIp = HttpContextHelper.GetClientIpAddress(HttpContext);
        var userAgent = Request.Headers.UserAgent.ToString();

        var command = new RefreshTokenCommand
        {
            RefreshToken = request.refreshToken,
            IpAddress = clientIp,
            UserAgent = userAgent
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequestDto request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new LogoutCommand
        {
            UserId = CurrentUser.UserId.Value,
            RefreshToken = request.refreshToken
        };
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}