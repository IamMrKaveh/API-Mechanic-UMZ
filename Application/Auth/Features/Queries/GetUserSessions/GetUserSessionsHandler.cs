using Application.Auth.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Queries.GetUserSessions;

public class GetUserSessionsHandler(
    IUserQueryService userQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUserSessionsQuery, PaginatedResult<UserSessionDto>>
{
    public async Task<ServiceResult<PaginatedResult<UserSessionDto>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken ct)
    {
        var effectiveId = request.TargetUserId ?? currentUserService.UserId
            ?? throw new InvalidOperationException("User context not resolved.");

        var userId = UserId.From(effectiveId);
        var sessions = await userQueryService.GetActiveSessionsAsync(userId, currentUserService.SessionId, ct);

        var list = sessions.ToList();
        var paginatedResult = PaginatedResult<UserSessionDto>.Create(
            list, list.Count, 1, int.MaxValue);

        return ServiceResult<PaginatedResult<UserSessionDto>>.Success(paginatedResult);
    }
}