using Application.Common.Results;
using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.UpdateUserAddress;

public record UpdateUserAddressCommand(UserId UserId, UserAddressId AddressId, UpdateUserAddressDto Dto) : IRequest<ServiceResult>;