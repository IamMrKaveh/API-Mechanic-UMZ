using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserAddresses;

public class GetUserAddressesHandler(
    IUserQueryService userQueryService)
        : IQueryHandler<GetUserAddressesQuery, IEnumerable<UserAddressDto>>
{
    public async Task<ServiceResult<IEnumerable<UserAddressDto>>> Handle(
        GetUserAddressesQuery request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var addresses = await userQueryService.GetUserAddressesAsync(userId, ct);
        return ServiceResult<IEnumerable<UserAddressDto>>.Success(addresses);
    }
}