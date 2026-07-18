using Application.User.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.User.Features.Queries.GetUserAddresses;

public class GetUserAddressesHandler(
    IUserQueryService userQueryService,
    ICurrentUserService currentUserService)
        : IQueryHandler<GetUserAddressesQuery, IEnumerable<UserAddressDto>>
{
    public async Task<ServiceResult<IEnumerable<UserAddressDto>>> Handle(
        GetUserAddressesQuery request, CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);

        var addresses = await userQueryService.GetUserAddressesAsync(userId, ct);
        return ServiceResult<IEnumerable<UserAddressDto>>.Success(addresses);
    }
}