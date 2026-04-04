using Application.Review.Features.Queries.GetUserReviews;
using Application.User.Features.Commands.ChangePassword;
using Application.User.Features.Commands.ChangePhoneNumber;
using Application.User.Features.Commands.CreateUserAddress;
using Application.User.Features.Commands.DeactivateAccount;
using Application.User.Features.Commands.DeleteUserAddress;
using Application.User.Features.Commands.UpdateProfile;
using Application.User.Features.Commands.UpdateUserAddress;
using Application.User.Features.Queries.GetCurrentUser;
using Application.User.Features.Queries.GetUserAddresses;
using Application.User.Features.Shared;
using media


namespace MainApi.User.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var query = new GetCurrentUserQuery(CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateRequest)
    {
        var command = new UpdateProfileCommand(
            CurrentUser.UserId,
            updateRequest.FirstName,
            updateRequest.LastName,
            updateRequest.Email);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var command = new DeactivateAccountCommand(CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetMyReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = new GetUserReviewsQuery(CurrentUser.UserId, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetUserAddresses()
    {
        var query = new GetUserAddressesQuery(CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] CreateUserAddressDto addressDto)
    {
        var result = await _mediator.Send(new CreateUserAddressCommand(CurrentUser.UserId, addressDto));
        return ToActionResult(result);
    }

    [HttpPut("addresses/{id}")]
    public async Task<IActionResult> UpdateAddress(int id, [FromBody] UpdateUserAddressDto addressDto)
    {
        var result = await _mediator.Send(new UpdateUserAddressCommand(CurrentUser.UserId, id, addressDto));
        return ToActionResult(result);
    }

    [HttpDelete("addresses/{id}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var result = await _mediator.Send(new DeleteUserAddressCommand(CurrentUser.UserId, id));
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(CurrentUser.UserId, dto));
        return ToActionResult(result);
    }

    [HttpPost("change-phone")]
    public async Task<IActionResult> ChangePhoneNumber([FromBody] ChangePhoneNumberRequest dto)
    {
        var command = new ChangePhoneNumberCommand
        {
            UserId = CurrentUser.UserId,
            NewPhoneNumber = dto.NewPhoneNumber,
            OtpCode = dto.OtpCode
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}