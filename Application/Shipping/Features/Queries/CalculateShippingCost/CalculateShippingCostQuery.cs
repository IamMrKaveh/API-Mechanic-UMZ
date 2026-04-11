namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public record CalculateShippingCostQuery(
    Guid UserId,
    Guid ShippingId) : IRequest<ServiceResult<ShippingCostResultDto>>;