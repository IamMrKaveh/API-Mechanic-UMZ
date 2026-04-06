using Application.Common.Results;
using Application.User.Features.Shared;

namespace Application.User.Features.Commands.CreateUserAddress;

public record CreateUserAddressCommand(
    Guid UserId,
    string Title,
    string ReceiverName,
    string PhoneNumber,
    string Province,
    string City,
    string Address,
    string PostalCode,
    bool IsDefault,
    decimal? Latitude,
    decimal? Longitude) : IRequest<ServiceResult<UserAddressDto>>;