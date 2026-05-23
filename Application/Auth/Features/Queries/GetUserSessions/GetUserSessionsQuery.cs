using Application.Auth.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(Guid UserId) : IRequest<ServiceResult<PaginatedResult<UserSessionDto>>>;