using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetCurrentUser;

public class GetCurrentUserHandler(IUserQueryService userQueryService) : IRequestHandler<GetCurrentUserQuery, ServiceResult<UserProfileDto>>
{
    private readonly IUserQueryService _userQueryService = userQueryService;

    public async Task<ServiceResult<UserProfileDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken ct)
    {
        var profile = await _userQueryService.GetUserProfileAsync(request.UserId, ct);
        if (profile is null)
            return ServiceResult<UserProfileDto>.NotFound("کاربر یافت نشد.");

        return ServiceResult<UserProfileDto>.Success(profile);
    }
}