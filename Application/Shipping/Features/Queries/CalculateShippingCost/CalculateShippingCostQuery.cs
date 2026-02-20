using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public record CalculateShippingCostQuery(int UserId, int ShippingMethodId)
    : IRequest<ServiceResult<ShippingCostResultDto>>;