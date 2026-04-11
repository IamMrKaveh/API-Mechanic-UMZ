using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using MediatR;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public class ApplyDiscountHandler(
IDiscountRepository discountRepository,
IUnitOfWork unitOfWork,
IAuditService auditService) : IRequestHandler<ApplyDiscountCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
    ApplyDiscountCommand request, CancellationToken ct)
    {
        return await unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var discount = await discountRepository.GetByCodeAsync(request.Code, ct);
                if (discount is null)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult.NotFound("کد تخفیف یافت نشد.");
                }

                var orderAmount = Money.FromDecimal(request.OrderAmount, "IRT");
                var validation = discount.ValidateForApplication(orderAmount);

                if (!validation.IsValid)
                {
                    await unitOfWork.RollbackTransactionAsync(ct);
                    return ServiceResult<DiscountApplicationResult>.Failure(validation.FailureReason!);
                }

                var discountAmount = discount.CalculateDiscount(orderAmount);
                var finalAmount = orderAmount.Subtract(discountAmount);

                var userId = UserId.From(request.UserId);
                var orderId = OrderId.From(request.OrderId);
                discount.RecordUsage(userId, orderId, discountAmount);

                discountRepository.Update(discount);
                await unitOfWork.SaveChangesAsync(ct);
                await unitOfWork.CommitTransactionAsync(ct);

                await auditService.LogOrderEventAsync(
                    orderId,
                    "DiscountApplied",
                    IpAddress.Unknown,
                    userId,
                    $"Discount {request.Code} applied. Amount: {discountAmount.Amount}",
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
                await auditService.LogSystemEventAsync("ApplyDiscountError", ex.Message);
                return ServiceResult<DiscountApplicationResult>.Failure("خطا در اعمال تخفیف");
            }
        }, ct);
    }
}