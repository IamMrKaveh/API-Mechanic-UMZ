using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Application.Payment.Contracts;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.Services.Strategies;

public sealed class ZarinPalCheckoutPaymentStrategy(
    IPaymentService paymentService,
    IOrderRepository orderRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICheckoutPaymentStrategy
{
    public string Code => "zarinpal";

    public async Task<ServiceResult<CheckoutResultDto>> ExecuteAsync(
        CheckoutResultDto orderResult,
        OrderId orderId,
        UserId userId,
        Money finalAmount,
        string ipAddress,
        string? userAgent,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        if (finalAmount.Amount <= 0)
            return await SettleFreeOrderAsync(orderResult, orderId, userId, idempotencyKey, ct);

        var ip = IpAddress.Create(string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress);

        var initResult = await paymentService.InitiatePaymentAsync(
            orderId, finalAmount, ip, userId, gatewayName: Code, ct);

        if (!initResult.IsSuccess)
            return ServiceResult<CheckoutResultDto>.Failure(initResult.Error ?? "خطا در ایجاد پرداخت.");

        return ServiceResult<CheckoutResultDto>.Success(orderResult with
        {
            PaymentUrl = initResult.Value?.PaymentUrl,
            PaymentAuthority = initResult.Value?.Authority,
            PaymentTransactionId = initResult.Value?.TransactionId,
            IsPaid = false,
            PaymentMethodCode = Code
        });
    }

    private async Task<ServiceResult<CheckoutResultDto>> SettleFreeOrderAsync(
        CheckoutResultDto orderResult,
        OrderId orderId,
        UserId userId,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<CheckoutResultDto>.NotFound("سفارش یافت نشد.");

        if (order.IsPaid)
        {
            return ServiceResult<CheckoutResultDto>.Success(orderResult with
            {
                PaymentTransactionId = order.PaymentTransactionId?.Value,
                IsPaid = true,
                PaymentMethodCode = Code
            });
        }

        var authority = $"FREE-ZP-{idempotencyKey:N}";
        var transaction = PaymentTransaction.Initiate(
            orderId,
            userId,
            authority,
            amount: 1m,
            "Zarinpal",
            dateTimeProvider.UtcNow,
            description: $"سفارش رایگان {order.OrderNumber.Value}");

        transaction.MarkAsSuccess(
            refId: DateTime.UtcNow.Ticks,
            now: dateTimeProvider.UtcNow,
            fee: 0);

        await paymentTransactionRepository.AddAsync(transaction, ct);

        order.MarkAsPaid(transaction.Id);
        orderRepository.Update(order);

        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<CheckoutResultDto>.Success(orderResult with
        {
            PaymentUrl = null,
            PaymentAuthority = authority,
            PaymentTransactionId = transaction.Id.Value,
            IsPaid = true,
            PaymentMethodCode = Code
        });
    }
}