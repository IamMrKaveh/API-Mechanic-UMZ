using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
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
using Presentation.User.Requests;

namespace Presentation.User.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/profile")]
[Authorize]
public sealed class ProfileController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var query = new GetCurrentUserQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetUserReviewsQuery(CurrentUser.UserId, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("addresses")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserAddressDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserAddresses(CancellationToken ct)
    {
        var query = new GetUserAddressesQuery(CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("addresses")]
    [ProducesResponseType(typeof(ApiResponse<UserAddressDto>), StatusCodes.Status201Created)]
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

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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

    [HttpPut("addresses/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var command = new DeactivateAccountCommand(CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("addresses/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        var command = new DeleteUserAddressCommand(CurrentUser.UserId, id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
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

    [HttpPatch("phone")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
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