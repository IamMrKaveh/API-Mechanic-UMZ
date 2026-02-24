namespace Application.Shipping.Features.Commands.CreateShipping;

public record CreateShippingCommand(
    string Name,
    decimal Cost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    bool IsActive,
    int CurrentUserId
    ) : IRequest<ServiceResult<ShippingDto>>;