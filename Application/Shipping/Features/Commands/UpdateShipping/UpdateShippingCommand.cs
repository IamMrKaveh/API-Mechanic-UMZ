namespace Application.Shipping.Features.Commands.UpdateShipping;

public record UpdateShippingCommand(
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