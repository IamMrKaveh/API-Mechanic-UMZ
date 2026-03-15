namespace Domain.Shipping.ValueObjects;

public sealed record ShippingAssignment(
    ShippingId ShippingId,
    decimal Weight,
    decimal Width,
    decimal Height,
    decimal Length);