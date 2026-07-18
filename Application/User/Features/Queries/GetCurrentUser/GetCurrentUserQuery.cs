using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetCurrentUser;

public record GetCurrentUserQuery()
    : IQuery<UserProfileDto>;