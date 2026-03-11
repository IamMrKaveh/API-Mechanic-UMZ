namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutPaymentProcessorService
{
    Task<ServiceResult<CheckoutResultDto>> ProcessAsync(
        Domain.Order.Aggregates.Order order,
        int userId,
        string gatewayName,
        string? callbackUrl,
        string idempotencyKey,
        CancellationToken ct);
}