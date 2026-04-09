using Application.Common.Results;
using Application.User.Features.Shared;

namespace Application.Auth.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(Guid UserId) : IRequest<ServiceResult<IEnumerable<UserSessionDto>>>;