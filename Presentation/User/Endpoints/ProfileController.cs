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
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var command = new UpdateProfileCommand(
            CurrentUser.UserId,
            request.FirstName,
            request.LastName,
            request.Email);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount()
    {
        var result = await _mediator.Send(new DeactivateAccountCommand(CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetUserReviewsQuery(CurrentUser.UserId, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetUserAddresses()
    {
        var result = await _mediator.Send(new GetUserAddressesQuery(CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] CreateUserAddressRequest request)
    {
        var command = new CreateUserAddressCommand(
            CurrentUser.UserId,
            request.Title,
            request.ReceiverName,
            request.PhoneNumber,
            request.Province,
            request.City,
            request.Address,
            request.PostalCode,
            request.IsDefault,
            request.Latitude,
            request.Longitude);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("addresses/{id}")]
    public async Task<IActionResult> UpdateAddress(
        Guid id,
        [FromBody] UpdateUserAddressRequest request)
    {
        var command = new UpdateUserAddressCommand(
            CurrentUser.UserId,
            id,
            request.Title,
            request.ReceiverName,
            request.PhoneNumber,
            request.Province,
            request.City,
            request.Address,
            request.PostalCode,
            request.IsDefault,
            request.Latitude,
            request.Longitude);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("addresses/{id}")]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        var result = await _mediator.Send(new DeleteUserAddressCommand(CurrentUser.UserId, id));
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var command = new ChangePasswordCommand(
            CurrentUser.UserId,
            request.CurrentPassword,
            request.NewPassword);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("change-phone")]
    public async Task<IActionResult> ChangePhoneNumber([FromBody] ChangePhoneNumberRequest request)
    {
        var command = new ChangePhoneNumberCommand(
            CurrentUser.UserId,
            request.NewPhoneNumber,
            request.OtpCode);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}