namespace Application.Order.Features.Commands.UpdateShippingMethod;

public record UpdateShippingMethodCommand(
    int Id,
    string Name,
    decimal Cost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    bool IsActive,
    string? RowVersion,
    int CurrentUserId) : IRequest<ServiceResult>;