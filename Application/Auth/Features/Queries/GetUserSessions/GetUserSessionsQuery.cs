using Application.User.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(Guid UserId) : IRequest<ServiceResult<PaginatedResult<UserSessionDto>>>;