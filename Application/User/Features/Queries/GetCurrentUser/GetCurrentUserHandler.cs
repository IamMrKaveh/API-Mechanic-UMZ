namespace Application.User.Features.Queries.GetCurrentUser;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, ServiceResult<UserProfileDto>>
{
    private readonly IUserQueryService _userQueryService;

    public GetCurrentUserHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<UserProfileDto>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var profile = await _userQueryService.GetUserProfileAsync(request.UserId, cancellationToken);
        if (profile == null)
            return ServiceResult<UserProfileDto>.Failure("کاربر یافت نشد.", 404);

        return ServiceResult<UserProfileDto>.Success(profile);
    }
}