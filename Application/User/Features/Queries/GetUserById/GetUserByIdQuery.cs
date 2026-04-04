using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserById;

public record GetUserByIdQuery(UserId Id) : IRequest<ServiceResult<UserProfileDto?>>;