using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<ServiceResult<UserProfileDto?>>;