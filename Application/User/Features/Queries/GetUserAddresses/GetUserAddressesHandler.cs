namespace Application.User.Features.Queries.GetUserAddresses;

public class GetUserAddressesHandler
    : IRequestHandler<GetUserAddressesQuery, ServiceResult<IEnumerable<UserAddressDto>>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUserAddressesHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<ServiceResult<IEnumerable<UserAddressDto>>> Handle(
        GetUserAddressesQuery request, CancellationToken cancellationToken)
    {
        var addresses = await _userQueryService.GetUserAddressesAsync(request.UserId, cancellationToken);
        return ServiceResult<IEnumerable<UserAddressDto>>.Success(addresses);
    }
}