namespace Application.Order.Features.Queries.CalculateShippingCost;

public record CalculateShippingCostQuery(int UserId, int ShippingMethodId)
    : IRequest<ServiceResult<ShippingCostResultDto>>;