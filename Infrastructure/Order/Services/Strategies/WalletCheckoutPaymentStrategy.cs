using Application.Common.Exceptions;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.Interfaces;
using Domain.User.ValueObjects;
using Domain.Wallet.Exceptions;
using Domain.Wallet.Interfaces;
using SharedKernel.Exceptions;

namespace Infrastructure.Order.Services.Strategies;

public sealed class WalletCheckoutPaymentStrategy(
    IWalletRepository walletRepository,
    IOrderRepository orderRepository,
    IPaymentTransactionRepository paymentTransactionRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider,
    IAuditService auditService) : ICheckoutPaymentStrategy
{
    public string Code => "wallet";

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
        try
        {
            if (finalAmount.Amount <= 0)
                return await SettleFreeOrderAsync(orderResult, orderId, userId, idempotencyKey, ct);

            var alreadyProcessed = await walletRepository
                .HasIdempotencyKeyAsync(userId, idempotencyKey.ToString(), ct);

            if (!alreadyProcessed)
            {
                var wallet = await walletRepository.GetByUserIdForUpdateAsync(userId, ct);
                if (wallet is null)
                    return ServiceResult<CheckoutResultDto>.NotFound("کیف پول یافت نشد.");

                wallet.Debit(
                    finalAmount,
                    $"پرداخت سفارش {orderResult.OrderNumber}",
                    $"ORDER-{orderId.Value}");

                walletRepository.Update(wallet);
            }

            return await SettlePaidOrderAsync(orderResult, orderId, userId, finalAmount, idempotencyKey, ct);
        }
        catch (InsufficientWalletBalanceException ex)
        {
            return ServiceResult<CheckoutResultDto>.Failure(ex.Message);
        }
        catch (WalletInactiveException)
        {
            return ServiceResult<CheckoutResultDto>.Failure("کیف پول شما غیرفعال است.");
        }
        catch (ConcurrencyException)
        {
            await auditService.LogSystemEventAsync(
                "WalletCheckoutConcurrencyConflict",
                $"تعارض همزمانی در پرداخت با کیف پول برای سفارش {orderId.Value}", ct);
            return ServiceResult<CheckoutResultDto>.Conflict("تعارض همزمانی رخ داد. لطفاً مجدداً تلاش کنید.");
        }
        catch (DomainException ex)
        {
            return ServiceResult<CheckoutResultDto>.Failure(ex.Message);
        }
    }

    private async Task<ServiceResult<CheckoutResultDto>> SettlePaidOrderAsync(
        CheckoutResultDto orderResult,
        OrderId orderId,
        UserId userId,
        Money finalAmount,
        Guid idempotencyKey,
        CancellationToken ct)
    {
        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<CheckoutResultDto>.NotFound("سفارش یافت نشد.");

        if (order.IsPaid)
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<CheckoutResultDto>.Success(orderResult with
            {
                PaymentUrl = null,
                PaymentAuthority = null,
                PaymentTransactionId = order.PaymentTransactionId?.Value,
                IsPaid = true,
                PaymentMethodCode = Code
            });
        }

        var authority = $"WALLET-{idempotencyKey:N}";
        var transaction = PaymentTransaction.Initiate(
            orderId,
            userId,
            authority,
            finalAmount.Amount,
            "Wallet",
            dateTimeProvider.UtcNow,
            description: $"پرداخت سفارش {order.OrderNumber.Value} از کیف پول");

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
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult<CheckoutResultDto>.Success(orderResult with
            {
                PaymentUrl = null,
                PaymentAuthority = null,
                PaymentTransactionId = order.PaymentTransactionId?.Value,
                IsPaid = true,
                PaymentMethodCode = Code
            });
        }

        var authority = $"FREE-{idempotencyKey:N}";
        var transaction = PaymentTransaction.Initiate(
            orderId,
            userId,
            authority,
            amount: 1m,
            "Wallet",
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