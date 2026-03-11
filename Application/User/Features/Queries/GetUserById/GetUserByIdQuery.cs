using Application.Common.Models;

namespace Application.User.Features.Queries.GetUserById;

public record GetUserByIdQuery(int Id) : IRequest<ServiceResult<UserProfileDto?>>;