using Domain.Discount.Interfaces;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutDiscountApplicatorService(
    IDiscountRepository discountRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork) : ICheckoutDiscountApplicatorService
{
    private readonly IDiscountRepository _discountRepository = discountRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> ApplyAsync(
        Domain.Order.Aggregates.Order order,
        string? discountCode,
        int userId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(discountCode))
            return ServiceResult.Success();

        var discount = await _discountRepository.GetByCodeAsync(discountCode, ct);
        if (discount == null)
            return ServiceResult.Failure("کد تخفیف نامعتبر است.");

        var userUsageCount = await _discountRepository.CountUserUsageAsync(discount.Id, userId, ct);
        var validation = discount.ValidateForApplication(order.TotalAmount.Amount, userId, userUsageCount);

        if (!validation.IsValid)
            return ServiceResult.Failure(validation.Error!);

        var discountMoney = discount.CalculateDiscountMoney(order.TotalAmount);
        discount.RecordUsage(userId, order.Id, discountMoney);
        order.ApplyDiscount(discount.Id, discountMoney);

        _discountRepository.Update(discount);
        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}