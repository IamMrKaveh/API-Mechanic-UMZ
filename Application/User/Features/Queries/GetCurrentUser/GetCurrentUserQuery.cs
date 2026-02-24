namespace Application.User.Features.Queries.GetCurrentUser;

public record GetCurrentUserQuery(int UserId) : IRequest<ServiceResult<UserProfileDto>>;