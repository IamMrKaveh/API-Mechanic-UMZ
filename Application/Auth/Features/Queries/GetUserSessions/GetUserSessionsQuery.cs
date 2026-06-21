using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10) : IPageQuery<UserSessionDto>;