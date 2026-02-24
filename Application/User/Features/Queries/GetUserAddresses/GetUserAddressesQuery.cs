namespace Application.User.Features.Queries.GetUserAddresses;

public record GetUserAddressesQuery(int UserId) : IRequest<ServiceResult<IEnumerable<UserAddressDto>>>;