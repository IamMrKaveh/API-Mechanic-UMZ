using Application.Common.Results;
using Application.Shipping.Features.Shared;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Commands.CreateShipping;

public record CreateShippingCommand(
    ShippingName Name,
    decimal Cost,
    string? Description,
    string? EstimatedDeliveryTime,
    int MinDeliveryDays,
    int MaxDeliveryDays,
    bool IsActive,
    UserId CurrentUserId) : IRequest<ServiceResult<ShippingDto>>;