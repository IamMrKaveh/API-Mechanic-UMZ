namespace Application.Product.Features.Queries.GetAllShippingMethods;

public class GetAllShippingMethodsHandler : IRequestHandler<GetAllShippingMethodsQuery, ServiceResult<IEnumerable<ShippingMethodDto>>>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;

    public GetAllShippingMethodsHandler(IShippingMethodRepository shippingMethodRepository)
    {
        _shippingMethodRepository = shippingMethodRepository;
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> Handle(GetAllShippingMethodsQuery request, CancellationToken cancellationToken)
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