using Application.Common.Models;

namespace Application.Auth.Features.Queries.GetUserSessions;

public class GetUserSessionsHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetUserSessionsQuery, ServiceResult<IEnumerable<UserSessionDto>>>
{
    private readonly IUserQueryService _userQueryService = userQueryService;

    public async Task<ServiceResult<IEnumerable<UserSessionDto>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken ct)
    {
        var sessions = await _userQueryService.GetUserSessionsAsync(request.UserId, ct);
        return ServiceResult<IEnumerable<UserSessionDto>>.Success(sessions);
    }
}