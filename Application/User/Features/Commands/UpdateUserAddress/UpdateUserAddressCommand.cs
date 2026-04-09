using Application.Common.Results;

namespace Application.User.Features.Commands.UpdateUserAddress;

public record UpdateUserAddressCommand(
    Guid UserId,
    Guid AddressId,
    string Title,
    string ReceiverName,
    string PhoneNumber,
    string Province,
    string City,
    string Address,
    string PostalCode,
    bool IsDefault,
    decimal? Latitude = null,
    decimal? Longitude = null) : IRequest<ServiceResult>;