using Application.User.Features.Commands.ChangePassword;
using Application.User.Features.Commands.CreateUserAddress;
using Application.User.Features.Commands.DeleteUserAddress;
using Application.User.Features.Commands.UpdateUserAddress;

namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : BaseApiController
{
    private readonly IMediator _mediator;

    public ProfileController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetCurrentUserQuery(CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateRequest)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new UpdateProfileCommand
        {
            UserId = CurrentUser.UserId.Value,
            FirstName = updateRequest.FirstName,
            LastName = updateRequest.LastName
        };
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new DeactivateAccountCommand(CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserReviewsQuery(CurrentUser.UserId.Value, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetUserAddresses()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new GetUserAddressesQuery(CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] CreateUserAddressDto addressDto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var result = await _mediator.Send(new CreateUserAddressCommand(CurrentUser.UserId.Value, addressDto));
        return ToActionResult(result);
    }

    [HttpPut("addresses/{id}")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] UpdateUserAddressDto addressDto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var result = await _mediator.Send(new UpdateUserAddressCommand(CurrentUser.UserId.Value, id, addressDto));
        return ToActionResult(result);
    }

    [HttpDelete("addresses/{id}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var result = await _mediator.Send(new DeleteUserAddressCommand(CurrentUser.UserId.Value, id));
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var result = await _mediator.Send(new ChangePasswordCommand(CurrentUser.UserId.Value, dto));
        return ToActionResult(result);
    }
}