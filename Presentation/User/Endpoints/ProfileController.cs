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

[Route("api/profile")]
[ApiController]
[Authorize]
public sealed class ProfileController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetCurrentUserQuery(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var command = new UpdateProfileCommand(
            CurrentUser.UserId,
            request.FirstName,
            request.LastName);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var result = await Mediator.Send(new DeactivateAccountCommand(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(
            new GetUserReviewsQuery(CurrentUser.UserId, page, pageSize), ct);
        return ToActionResult(result);
    }

    [HttpGet("addresses")]
    public async Task<IActionResult> GetUserAddresses(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetUserAddressesQuery(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress(
        [FromBody] CreateUserAddressRequest request,
        CancellationToken ct)
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

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPut("addresses/{id:guid}")]
    public async Task<IActionResult> UpdateAddress(
        Guid id,
        [FromBody] UpdateUserAddressRequest request,
        CancellationToken ct)
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

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("addresses/{id:guid}")]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new DeleteUserAddressCommand(CurrentUser.UserId, id), ct);
        return ToActionResult(result);
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        var command = new ChangePasswordCommand(
            CurrentUser.UserId,
            request.CurrentPassword,
            request.NewPassword);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("change-phone")]
    public async Task<IActionResult> ChangePhoneNumber(
        [FromBody] ChangePhoneNumberRequest request,
        CancellationToken ct)
    {
        var command = new ChangePhoneNumberCommand(
            CurrentUser.UserId,
            request.NewPhoneNumber,
            request.OtpCode);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}