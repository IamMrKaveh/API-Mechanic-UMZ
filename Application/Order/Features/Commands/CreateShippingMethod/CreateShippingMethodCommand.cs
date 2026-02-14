namespace Application.Order.Features.Commands.CreateShippingMethod;

public record CreateShippingMethodCommand(
    string Name,
    decimal Cost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    bool IsActive,
    int CurrentUserId) : IRequest<ServiceResult<ShippingMethodDto>>;