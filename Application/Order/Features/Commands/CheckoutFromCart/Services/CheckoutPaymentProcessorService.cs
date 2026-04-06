using Application.Common.Results;
using Application.Order.Features.Shared;
using Application.Payment.Contracts;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public class CheckoutPaymentProcessorService(IPaymentService paymentService)
    : ICheckoutPaymentProcessorService
{
    public async Task<ServiceResult<CheckoutResultDto>> ProcessAsync(
        CheckoutResultDto orderResult,
        string? paymentMethod,
        string ipAddress,
        string? userAgent,
        CancellationToken ct)
    {
        if (orderResult.FinalAmount <= 0)
            return ServiceResult<CheckoutResultDto>.Success(orderResult);

        var paymentResult = await paymentService.InitiatePaymentAsync(
            orderResult.OrderId, orderResult.FinalAmount, ipAddress, ct);

        if (!paymentResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(paymentResult.Error ?? "خطا در ایجاد پرداخت.");

        return ServiceResult<CheckoutResultDto>.Success(orderResult with
        {
            PaymentUrl = paymentResult.Value?.PaymentUrl,
            PaymentAuthority = paymentResult.Value?.Authority
        });
    }
}