using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
using Application.User.Features.Commands.ChangePassword;
using Application.User.Features.Commands.ChangePhoneNumber;
using Application.User.Features.Commands.DeactivateAccount;
using Application.User.Features.Commands.UpdateProfile;
using Application.User.Features.Queries.GetCurrentUser;
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

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var command = new DeactivateAccountCommand(CurrentUser.UserId);
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