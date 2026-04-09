namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public record CalculateShippingCostQuery(Guid UserId, Guid ShippingMethodId) : IRequest<ServiceResult<ShippingCostResultDto>>;