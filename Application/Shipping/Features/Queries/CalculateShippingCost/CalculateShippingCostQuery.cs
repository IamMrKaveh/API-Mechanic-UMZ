using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public sealed record CalculateShippingCostQuery(
    Guid ShippingId,
    decimal OrderAmount) : IRequest<ServiceResult<ShippingCostResultDto>>;