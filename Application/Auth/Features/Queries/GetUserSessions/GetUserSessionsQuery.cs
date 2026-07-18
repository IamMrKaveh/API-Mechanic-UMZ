using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(Guid? TargetUserId = null)
    : IQuery<PaginatedResult<UserSessionDto>>;