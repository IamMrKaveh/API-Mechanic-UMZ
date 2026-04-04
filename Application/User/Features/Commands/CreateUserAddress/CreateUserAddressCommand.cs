using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.CreateUserAddress;

public record CreateUserAddressCommand(
    UserId UserId,
    CreateUserAddressDto Dto) : IRequest<ServiceResult<int>>;