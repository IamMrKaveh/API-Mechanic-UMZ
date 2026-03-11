using Application.Common.Models;

namespace Application.User.Features.Queries.GetCurrentUser;

public record GetCurrentUserQuery(int UserId) : IRequest<ServiceResult<UserProfileDto>>;