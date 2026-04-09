using Application.User.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public class GetUserSessionsHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetUserSessionsQuery, ServiceResult<PaginatedResult<UserSessionDto>>>
{
    public async Task<ServiceResult<IEnumerable<UserSessionDto>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken ct)
    {
        var sessions = await userQueryService.GetUserSessionsAsync(request.UserId, ct);
        return ServiceResult<IEnumerable<UserSessionDto>>.Success(sessions);
    }
}