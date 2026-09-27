using Application.Discount.Features.Shared;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public class ApplyDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ApplyDiscountCommand>
{
    public async Task<ServiceResult> Handle(
        ApplyDiscountCommand request, CancellationToken ct)
    {
        try
        {
            return await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
            {
                var discountResult = await (discountRepository.GetByCodeAsync(request.Code, cancellationToken)).OrNotFoundAsync("کد تخفیف یافت نشد.");
                if (discountResult.IsFailure) return ServiceResult.Failure(discountResult.Error);
                var discount = discountResult.Value;

                var orderAmount = Money.FromDecimal(request.OrderAmount, "IRT");
                var now = dateTimeProvider.UtcNow;
                var validation = discount.ValidateForApplication(orderAmount, now);
                if (!validation.IsValid)
                    return ServiceResult<DiscountApplicationResult>.Failure(validation.FailureReason!);

                var discountAmount = discount.CalculateDiscount(orderAmount);
                var finalAmount = orderAmount.Subtract(discountAmount);
                var userId = UserId.From(currentUserService.UserId!.Value);
                var orderId = OrderId.From(request.OrderId);

                discount.RecordUsage(userId, orderId, discountAmount, now);
                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await auditService.LogOrderEventAsync(
                    orderId,
                    "DiscountApplied",
                    IpAddress.Unknown,
                    userId,
                    $"Discount {request.Code} applied. Amount: {discountAmount.Amount}",
                    cancellationToken);

                return ServiceResult<DiscountApplicationResult>.Success(new DiscountApplicationResult
                {
                    IsSuccess = true,
                    DiscountAmount = discountAmount.Amount,
                    FinalAmount = finalAmount.Amount
                });
            }, ct);
        }
        catch (Exception ex)
        {
            await auditService.LogSystemEventAsync("ApplyDiscountError", ex.Message, ct);
            return ServiceResult<DiscountApplicationResult>.Failure("خطا در اعمال تخفیف");
        }
    }
}
