namespace Presentation.Shipping.Requests;

public record CreateShippingRequest(
    string Name,
    decimal BaseCost,
    string? Description = null,
    string? EstimatedDeliveryTime = null,
    int MinDeliveryDays = 1,
    int MaxDeliveryDays = 7
);

public record UpdateShippingRequest(
    Guid Id,
    string Name,
    decimal BaseCost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    string RowVersion
);