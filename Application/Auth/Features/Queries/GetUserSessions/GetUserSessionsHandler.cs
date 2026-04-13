using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Queries.GetUserSessions;

public class GetUserSessionsHandler(IUserQueryService userQueryService)
    : IRequestHandler<GetUserSessionsQuery, ServiceResult<PaginatedResult<UserSessionDto>>>
{
    public async Task<ServiceResult<PaginatedResult<UserSessionDto>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var sessions = await userQueryService.GetActiveSessionsAsync(userId, ct);

        var paginatedResult = PaginatedResult<UserSessionDto>.Create(
            sessions.ToList(),
            sessions.Count(),
            1,
            int.MaxValue);

        return ServiceResult<PaginatedResult<UserSessionDto>>.Success(paginatedResult);
    }
}