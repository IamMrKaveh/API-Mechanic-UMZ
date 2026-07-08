using Application.Payment.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandler(
    IOrderRepository orderRepository,
    ICurrentUserService currentUser,
    IPaymentService paymentService)
    : ICommandHandler<InitiatePaymentCommand, PaymentInitiationResult>
{
    public async Task<ServiceResult<PaymentInitiationResult>> Handle(
        InitiatePaymentCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return ServiceResult<PaymentInitiationResult>.Forbidden("کاربر شناسایی نشد.");

        var orderId = OrderId.From(request.OrderId);
        var userId = UserId.From(currentUser.UserId.Value);

        var order = await orderRepository.FindByIdAsync(orderId, ct);
        if (order is null)
            return ServiceResult<PaymentInitiationResult>.NotFound("سفارش یافت نشد.");

        if (order.UserId != userId)
            return ServiceResult<PaymentInitiationResult>.Forbidden("دسترسی ممنوع.");

        if (order.IsPaid)
            return ServiceResult<PaymentInitiationResult>.Conflict("سفارش قبلاً پرداخت شده است.");

        var ipAddress = IpAddress.Create(currentUser.IpAddress ?? IpAddress.Unknown.Value);

        return await paymentService.InitiatePaymentAsync(orderId, order.FinalAmount, ipAddress, userId, "", ct);
    }
}