namespace Application.Order.Features.Shared;

public record CheckoutResultDto
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal FinalAmount { get; init; }
    public string? PaymentUrl { get; init; }
    public string? PaymentAuthority { get; init; }
    public Guid? PaymentTransactionId { get; init; }
    public bool IsPaid { get; init; }
    public string? PaymentMethodCode { get; init; }
}