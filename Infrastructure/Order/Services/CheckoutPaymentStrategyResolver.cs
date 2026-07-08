using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Order.Services;

public sealed class CheckoutPaymentStrategyResolver(
    IEnumerable<ICheckoutPaymentStrategy> strategies,
    IPaymentMethodRepository paymentMethodRepository) : ICheckoutPaymentStrategyResolver
{
    private readonly IReadOnlyDictionary<string, ICheckoutPaymentStrategy> _byCode =
        strategies.ToDictionary(s => s.Code.ToLowerInvariant(), s => s);

    public async Task<ServiceResult<ICheckoutPaymentStrategy>> ResolveAsync(
        Guid? paymentMethodId,
        string? paymentGateway,
        CancellationToken ct)
    {
        string? code = null;

        if (paymentMethodId.HasValue && paymentMethodId.Value != Guid.Empty)
        {
            var method = await paymentMethodRepository
                .GetByIdAsync(PaymentMethodId.From(paymentMethodId.Value), ct);

            if (method is null)
                return ServiceResult<ICheckoutPaymentStrategy>.NotFound("روش پرداخت انتخابی یافت نشد.");

            if (!method.IsActive || method.IsDeleted)
                return ServiceResult<ICheckoutPaymentStrategy>.Failure("روش پرداخت انتخابی غیرفعال است.");

            code = method.Code.Value;
        }
        else if (!string.IsNullOrWhiteSpace(paymentGateway))
        {
            code = paymentGateway;
        }

        if (string.IsNullOrWhiteSpace(code))
            return ServiceResult<ICheckoutPaymentStrategy>.Failure("روش پرداخت مشخص نشده است.");

        var normalized = code.Trim().ToLowerInvariant();

        if (_byCode.TryGetValue(normalized, out var strategy))
            return ServiceResult<ICheckoutPaymentStrategy>.Success(strategy);

        if (normalized is "zarinpalsandbox" or "zarinpal"
            && _byCode.TryGetValue("zarinpal", out var zp))
            return ServiceResult<ICheckoutPaymentStrategy>.Success(zp);

        return ServiceResult<ICheckoutPaymentStrategy>.Failure($"روش پرداخت '{code}' پشتیبانی نمی‌شود.");
    }
}