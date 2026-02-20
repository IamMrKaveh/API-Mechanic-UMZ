using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;

namespace Application.Product.Features.Queries.GetAllShipping;

public class GetAllShippingHandler : IRequestHandler<GetAllShippingQuery, ServiceResult<IEnumerable<ShippingMethodDto>>>
{
    private readonly IShippingRepository _shippingMethodRepository;

    public GetAllShippingHandler(IShippingRepository shippingMethodRepository)
    {
        _shippingMethodRepository = shippingMethodRepository;
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> Handle(GetAllShippingQuery request, CancellationToken cancellationToken)
    {
        var methods = await _shippingMethodRepository.GetAllAsync(false);

        var dtos = methods.Select(m => new ShippingMethodDto
        {
            Id = m.Id,
            Name = m.Name,
            Cost = m.Cost,
            Description = m.Description,
            EstimatedDeliveryTime = m.EstimatedDeliveryTime,
            IsActive = m.IsActive,
            IsDeleted = m.IsDeleted,
            RowVersion = m.RowVersion != null ? Convert.ToBase64String(m.RowVersion) : null
        });

        return ServiceResult<IEnumerable<ShippingMethodDto>>.Success(dtos);
    }
}