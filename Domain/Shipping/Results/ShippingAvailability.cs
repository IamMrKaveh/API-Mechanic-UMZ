using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Results;

public sealed record ShippingAvailability(
    ShippingId ShippingId,
    string Name,
    string? Description,
    Money Cost,
    bool IsFree,
    bool IsAvailable,
    bool IsDefault,
    string DeliveryTimeDisplay,
    string? UnavailableReason);