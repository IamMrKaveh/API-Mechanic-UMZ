using Application.Audit.Contracts;
using Application.Common.Results;
using Application.Discount.Contracts;
using Application.Discount.Features.Shared;
using Domain.Common.Interfaces;
using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Discount.Services;

public class DiscountService(
IDiscountRepository discountRepository,
IUnitOfWork unitOfWork,
IAuditService auditService) : IDiscountService
{
    public async Task<ServiceResult<DiscountApplicationResult>> ApplyDiscountAsync(
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
                    return ServiceResult<DiscountApplicationResult>.Failure("کد تخفیف نامعتبر است.");
                }

                var validation = discount.ValidateForApplication(orderAmount);

                if (!validation.IsValid)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult<DiscountApplicationResult>.Failure(validation.FailureReason!);
                }

                var discountAmount = discount.CalculateDiscount(orderAmount);
                var finalAmount = orderAmount.Subtract(discountAmount);

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

                return ServiceResult<DiscountApplicationResult>.Success(new DiscountApplicationResult
                {
                    IsSuccess = true,
                    DiscountAmount = discountAmount.Amount,
                    FinalAmount = finalAmount.Amount
                });
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                await auditService.LogSystemEventAsync("DiscountApplyFailed", ex.Message, userId.Value);
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
                var usage = await discountRepository.GetUsageByOrderIdAsync(orderId.Value.GetHashCode(), ct);
                if (usage == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Success();
                }

                var discount = await discountRepository.GetByIdWithUsagesAsync(usage.DiscountCodeId.Value.GetHashCode(), ct);
                if (discount == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure("کد تخفیف یافت نشد.");
                }

                discount.RemoveRestriction(Domain.Discount.ValueObjects.DiscountRestrictionId.From(usage.Id.Value));
                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogSystemEventAsync("DiscountUsageCancelled", $"Discount usage cancelled for order {orderId.Value}");

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
                var usage = await discountRepository.GetUsageByOrderIdAsync(orderId.Value.GetHashCode(), ct);
                if (usage == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Success();
                }

                var discount = await discountRepository.GetByIdWithUsagesAsync(usage.DiscountCodeId.Value.GetHashCode(), ct);
                if (discount == null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.Failure("کد تخفیف یافت نشد.");
                }

                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogSystemEventAsync("DiscountUsageConfirmed", $"Discount usage confirmed for order {orderId.Value}");

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