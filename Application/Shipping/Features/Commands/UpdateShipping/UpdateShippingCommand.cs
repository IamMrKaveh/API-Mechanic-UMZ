using Application.Common.Results;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Commands.UpdateShipping;

public record UpdateShippingCommand(
    ShippingId Id,
    string Name,
    decimal Cost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    bool IsActive,
    string? RowVersion,
    Guid CurrentUserId) : IRequest<ServiceResult>;