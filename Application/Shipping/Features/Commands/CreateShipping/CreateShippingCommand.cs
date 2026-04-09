using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Commands.CreateShipping;

public record CreateShippingCommand(
    string Name,
    decimal BaseCost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays) : IRequest<ServiceResult<ShippingDto>>;