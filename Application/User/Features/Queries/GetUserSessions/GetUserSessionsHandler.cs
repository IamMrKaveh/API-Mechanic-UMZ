namespace Application.User.Features.Queries.GetUserSessions;

public class GetUserSessionsHandler
    : IRequestHandler<GetUserSessionsQuery, ServiceResult<IEnumerable<UserSessionDto>>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUserSessionsHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<IEnumerable<UserSessionDto>>> Handle(
        GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _userQueryService.GetActiveSessionsAsync(request.UserId, cancellationToken);
        return ServiceResult<IEnumerable<UserSessionDto>>.Success(sessions);
    }
}