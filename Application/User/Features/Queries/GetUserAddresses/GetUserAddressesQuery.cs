using Application.User.Features.Shared;

namespace Application.User.Features.Queries.GetUserAddresses;

public record GetUserAddressesQuery()
    : IQuery<IEnumerable<UserAddressDto>>;