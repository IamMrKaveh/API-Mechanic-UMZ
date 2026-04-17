using Application.Discount.Contracts;
using Domain.Common.Interfaces;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Discount.Services;

public sealed class DiscountService(
    IDiscountRepository discountRepository,
    DBContext context,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IDiscountService
{
    public async Task<ServiceResult> ApplyDiscountAsync(
        string code,
        Money orderAmount,
        UserId userId,
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var discount = await discountRepository.GetByCodeAsync(code, ct);
                if (discount == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure("کد تخفیف نامعتبر است.");
                }

                var validation = discount.ValidateForApplication(orderAmount);
                if (!validation.IsValid)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure(validation.FailureReason!);
                }

                var discountAmount = discount.CalculateDiscount(orderAmount);
                discount.RecordUsage(userId, orderId, discountAmount);
                discountRepository.Update(discount);

                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogOrderEventAsync(
                    orderId,
                    "DiscountApplied",
                    IpAddress.Unknown,
                    userId,
                    $"Discount {code} applied. Amount: {discountAmount.Amount}",
                    ct);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                await auditService.LogSystemEventAsync("DiscountApplyFailed", ex.Message, ct);
                throw;
            }
        }, ct);
    }

    public async Task<ServiceResult> CancelDiscountUsageAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var discount = await context.DiscountCodes
                    .Include(d => d.Usages)
                    .FirstOrDefaultAsync(d => d.Usages.Any(u => u.OrderId == orderId), ct);

                if (discount == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Success();
                }

                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogSystemEventAsync(
                    "DiscountUsageCancelled",
                    $"Discount usage cancelled for order {orderId.Value}");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                await auditService.LogSystemEventAsync("DiscountCancelFailed", ex.Message);
                throw;
            }
        }, ct);
    }

    public async Task<ServiceResult> ConfirmDiscountUsageAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var discount = await context.DiscountCodes
                    .Include(d => d.Usages)
                    .FirstOrDefaultAsync(d => d.Usages.Any(u => u.OrderId == orderId), ct);

                if (discount == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Success();
                }

                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogSystemEventAsync(
                    "DiscountUsageConfirmed",
                    $"Discount usage confirmed for order {orderId.Value}");

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                await auditService.LogSystemEventAsync("DiscountConfirmFailed", ex.Message);
                throw;
            }
        }, ct);
    }
}