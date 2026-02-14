namespace Application.User.Features.Queries.GetUserSessions;

public record GetUserSessionsQuery(int UserId) : IRequest<ServiceResult<IEnumerable<UserSessionDto>>>;