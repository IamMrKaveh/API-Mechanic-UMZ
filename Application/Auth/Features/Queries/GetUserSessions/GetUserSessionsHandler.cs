using Application.Auth.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Auth.Features.Queries.GetUserSessions;

public sealed class GetUserSessionsHandler(
    IUserQueryService userQueryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetUserSessionsQuery, PaginatedResult<UserSessionDto>>
{
    public async Task<ServiceResult<PaginatedResult<UserSessionDto>>> Handle(
        GetUserSessionsQuery request,
        CancellationToken ct)
    {
        var effectiveId = request.TargetUserId ?? currentUserService.UserId;

        if (effectiveId is null || effectiveId == Guid.Empty)
            return ServiceResult<PaginatedResult<UserSessionDto>>.Unauthorized(
                "کاربر احراز هویت نشده است.");

        var userId = UserId.From(effectiveId.Value);

        var sessions = await userQueryService.GetActiveSessionsAsync(
            userId,
            currentUserService.SessionId,
            ct);

        var list = sessions.ToList();
        var count = list.Count;
        var pageSize = count > 0 ? count : 1;

        var paginated = PaginatedResult<UserSessionDto>.Create(list, count, 1, pageSize);

        return ServiceResult<PaginatedResult<UserSessionDto>>.Success(paginated);
    }
}
