using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(Guid UserId, Guid? CurrentSessionId = null)
    : IQuery<PaginatedResult<UserSessionDto>>;