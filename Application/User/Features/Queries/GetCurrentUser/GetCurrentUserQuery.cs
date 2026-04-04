using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetCurrentUser;

public record GetCurrentUserQuery(UserId UserId) : IRequest<ServiceResult<UserProfileDto>>;