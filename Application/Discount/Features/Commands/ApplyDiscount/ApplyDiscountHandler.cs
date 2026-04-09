using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Discount.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public class ApplyDiscountHandler(
    IDiscountRepository discountRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ApplyDiscountCommand, ServiceResult<DiscountApplicationResult>>
{
    public async Task<ServiceResult<DiscountApplicationResult>> Handle(
        ApplyDiscountCommand request, CancellationToken ct)
    {
        var discount = await discountRepository.GetByCodeAsync(request.Code, ct);
        if (discount is null)
            return ServiceResult<DiscountApplicationResult>.NotFound("کد تخفیف یافت نشد.");

        var orderAmount = Money.FromDecimal(request.OrderAmount);
        var validation = discount.ValidateForApplication(orderAmount);

        if (!validation.IsValid)
            return ServiceResult<DiscountApplicationResult>.Failure(validation.FailureReason!);

        var discountAmount = discount.CalculateDiscount(orderAmount);
        var finalAmount = orderAmount.Subtract(discountAmount);

        var userId = UserId.From(request.UserId);
        var orderId = OrderId.From(request.OrderId);
        discount.RecordUsage(userId, orderId, discountAmount);

        discountRepository.Update(discount);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<DiscountApplicationResult>.Success(new DiscountApplicationResult
        {
            IsSuccess = true,
            DiscountAmount = discountAmount.Amount,
            FinalAmount = finalAmount.Amount
        });
    }
}