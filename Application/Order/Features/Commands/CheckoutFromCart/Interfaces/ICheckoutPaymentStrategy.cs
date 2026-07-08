using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.User.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Interfaces;

public interface ICheckoutPaymentStrategy
{
    string Code { get; }

    Task<ServiceResult<CheckoutResultDto>> ExecuteAsync(
        CheckoutResultDto orderResult,
        OrderId orderId,
        UserId userId,
        Money finalAmount,
        string ipAddress,
        string? userAgent,
        Guid idempotencyKey,
        CancellationToken ct);
}

public interface ICheckoutPaymentStrategyResolver
{
    Task<ServiceResult<ICheckoutPaymentStrategy>> ResolveAsync(
        Guid? paymentMethodId,
        string? paymentGateway,
        CancellationToken ct);
}