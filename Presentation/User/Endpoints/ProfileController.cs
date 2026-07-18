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
        return await Send(new GetCurrentUserQuery(), ct);
    }

    [HttpGet("reviews")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return await Send(new GetUserReviewsQuery(page, pageSize), ct);
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        return await Send(new UpdateProfileCommand(request.FirstName, request.LastName), ct);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        return await Send(new DeactivateAccountCommand(), ct);
    }

    [HttpPatch("password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken ct)
    {
        return await Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), ct);
    }

    [HttpPatch("phone")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePhoneNumber(
        [FromBody] ChangePhoneNumberRequest request,
        CancellationToken ct)
    {
        return await Send(new ChangePhoneNumberCommand(request.NewPhoneNumber, request.OtpCode), ct);
    }
}