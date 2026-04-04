using Application.Common.Results;
using Domain.User.ValueObjects;

namespace Application.User.Features.Commands.DeleteUserAddress;

public record DeleteUserAddressCommand(UserId UserId, UserAddressId AddressId) : IRequest<ServiceResult>;