using Application.Common.Results;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Commands.UpdateShipping;

public record UpdateShippingCommand(
    Guid Id,
    string Name,
    decimal BaseCost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays) : IRequest<ServiceResult<ShippingDto>>;