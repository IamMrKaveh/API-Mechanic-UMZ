using Application.Cart;
using Domain.User.Interfaces;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutPaymentProcessorService(
    IWalletService walletService,
    IPaymentService paymentService,
    IInventoryService inventoryService,
    ICartRepository cartRepository,
    IUserRepository userRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<CheckoutPaymentProcessorService> logger) : ICheckoutPaymentProcessorService
{
    private readonly IWalletService _walletService = walletService;
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<CheckoutPaymentProcessorService> _logger = logger;

    public async Task<ServiceResult<CheckoutResultDto>> ProcessAsync(
        Domain.Order.Aggregates.Order order,
        int userId,
        string gatewayName,
        string? callbackUrl,
        string idempotencyKey,
        CancellationToken ct)
    {
        if (gatewayName.Equals("Wallet", StringComparison.OrdinalIgnoreCase))
            return await ProcessWalletPaymentAsync(order, userId, idempotencyKey, ct);

        return await ProcessGatewayPaymentAsync(order, userId, callbackUrl, ct);
    }

    private async Task<ServiceResult<CheckoutResultDto>> ProcessWalletPaymentAsync(
        Domain.Order.Aggregates.Order order,
        int userId,
        string idempotencyKey,
        CancellationToken ct)
    {
        var debitResult = await _walletService.DebitAsync(
            userId: userId,
            amount: order.FinalAmount.Amount,
            transactionType: WalletTransactionType.OrderPayment,
            referenceType: WalletReferenceType.Order,
            referenceId: order.Id,
            idempotencyKey: $"order-pay-{order.Id}-{idempotencyKey}",
            ct: ct);

        if (debitResult.IsFailed)
            return ServiceResult<CheckoutResultDto>.Failure(debitResult.Error ?? "خطا در کسر از کیف پول.");

        order.MarkAsPaid(refId: 0, cardPan: "Wallet");
        order.StartProcessing();

        await _orderRepository.UpdateAsync(order, ct);
        await _cartRepository.ClearCartAsync(userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} paid via Wallet for user {UserId}.", order.Id, userId);

        return ServiceResult<CheckoutResultDto>.Success(
            new CheckoutResultDto(order.Id, null, null, null, true));
    }

    private async Task<ServiceResult<CheckoutResultDto>> ProcessGatewayPaymentAsync(
        Domain.Order.Aggregates.Order order,
        int userId,
        string? callbackUrl,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        var paymentResult = await _paymentService.InitiatePaymentAsync(new PaymentInitiationDto
        {
            OrderId = order.Id,
            UserId = userId,
            Amount = order.FinalAmount,
            Description = $"پرداخت سفارش #{order.Id}",
            CallbackUrl = callbackUrl ?? "",
            Mobile = user?.PhoneNumber
        });

        if (paymentResult.IsFailed)
        {
            await _inventoryService.RollbackReservationsAsync($"ORDER-{order.Id}");
            return ServiceResult<CheckoutResultDto>.Failure(
                paymentResult.Error ?? "خطا در ایجاد درخواست پرداخت");
        }

        await _cartRepository.ClearCartAsync(userId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Order {OrderId} created successfully for user {UserId}. OrderNumber: {OrderNumber}",
            order.Id, userId, order.OrderNumber.Value);

        return ServiceResult<CheckoutResultDto>.Success(
            new CheckoutResultDto(order.Id, paymentResult.Value.PaymentUrl, paymentResult.Value.Authority, null, false));
    }
}