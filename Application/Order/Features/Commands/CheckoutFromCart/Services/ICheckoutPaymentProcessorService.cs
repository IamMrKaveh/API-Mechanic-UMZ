using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutPaymentProcessorService
{
    Task<ServiceResult<CheckoutResultDto>> ProcessAsync(
        CheckoutResultDto orderResult,
        string? paymentMethod,
        string ipAddress,
        string? userAgent,
        CancellationToken ct);
}