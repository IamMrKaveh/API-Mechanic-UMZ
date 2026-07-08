using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;

namespace Infrastructure.Order.Services;

public sealed class CheckoutPaymentStrategyResolver(
    IEnumerable<ICheckoutPaymentStrategy> strategies,
    IPaymentMethodRepository paymentMethodRepository) : ICheckoutPaymentStrategyResolver
{
    private readonly IReadOnlyDictionary<string, ICheckoutPaymentStrategy> _byCode =
        strategies.ToDictionary(s => Canonicalize(s.Code), s => s);

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

        var canonical = Canonicalize(code);

        if (_byCode.TryGetValue(canonical, out var strategy))
            return ServiceResult<ICheckoutPaymentStrategy>.Success(strategy);

        var aliasTarget = ResolveAlias(canonical);
        if (aliasTarget is not null && _byCode.TryGetValue(aliasTarget, out var aliased))
            return ServiceResult<ICheckoutPaymentStrategy>.Success(aliased);

        return ServiceResult<ICheckoutPaymentStrategy>.Failure(
            $"روش پرداخت '{code}' پشتیبانی نمی‌شود.");
    }

    private static string Canonicalize(string value)
        => value.Trim().ToLowerInvariant().Replace('_', '-');

    private static string? ResolveAlias(string canonicalCode)
        => canonicalCode switch
        {
            "zarinpal-sandbox" => PaymentMethodCode.Zarinpal,
            "zarinpalsandbox" => PaymentMethodCode.Zarinpal,
            "zarinpal" => PaymentMethodCode.Zarinpal,
            "cashondelivery" => PaymentMethodCode.CashOnDelivery,
            "cash-on-delivery" => PaymentMethodCode.CashOnDelivery,
            "wallet" => PaymentMethodCode.Wallet,
            _ => null
        };
}