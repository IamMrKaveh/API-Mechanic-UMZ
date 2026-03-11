using Application.Common.Models;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(int UserId) : IRequest<ServiceResult<IEnumerable<UserSessionDto>>>;