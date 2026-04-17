using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Common.Interfaces;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.Services;

public sealed class CheckoutDiscountApplicatorService(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : ICheckoutDiscountApplicatorService
{
    public async Task<ServiceResult<(Money DiscountAmount, Guid? DiscountCodeId)>> ApplyAsync(
        string? discountCode, Money orderAmount, Guid userId, CancellationToken ct)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                if (string.IsNullOrWhiteSpace(discountCode))
                {
                    await unitOfWork.CommitTransactionAsync(ct);
                    return ServiceResult<(Money, Guid?)>.Success((Money.Zero(orderAmount.Currency), null));
                }

                var discount = await discountRepository.GetByCodeAsync(discountCode, ct);
                if (discount is null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult<(Money, Guid?)>.NotFound("کد تخفیف یافت نشد.");
                }

                var validation = discount.ValidateForApplication(orderAmount);
                if (!validation.IsValid)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult<(Money, Guid?)>.Failure(validation.FailureReason!);
                }

                var tempOrderId = OrderId.NewId();
                var discountAmount = discount.CalculateDiscount(orderAmount);
                discount.RecordUsage(UserId.From(userId), tempOrderId, discountAmount);

                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogSystemEventAsync(
                    "CheckoutDiscountApplied",
                    $"Discount {discountCode} applied during checkout",
                    ct);

                return ServiceResult<(Money, Guid?)>.Success((discountAmount, discount.Id.Value));
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                await auditService.LogSystemEventAsync("CheckoutDiscountError", ex.Message, ct);
                return ServiceResult<(Money, Guid?)>.Failure("خطا در اعمال تخفیف");
            }
        }, ct);
    }
}