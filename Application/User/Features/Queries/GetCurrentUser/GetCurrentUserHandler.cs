using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetCurrentUser;

public class GetCurrentUserHandler(
    IUserQueryService userQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentUserQuery, UserProfileDto>
{
    public async Task<ServiceResult<UserProfileDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);

        var profile = await userQueryService.GetUserProfileAsync(userId, ct);
        if (profile is null)
            return ServiceResult<UserProfileDto>.NotFound("کاربر یافت نشد.");

        return ServiceResult<UserProfileDto>.Success(profile);
    }
}