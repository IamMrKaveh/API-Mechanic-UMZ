namespace Application.Payment.Features.Commands.AtomicRefundPayment;

public sealed class AtomicRefundPaymentHandler : IRequestHandler<AtomicRefundPaymentCommand, AtomicRefundResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentTransactionRepository _paymentRepo;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PaymentSettlementService _settlementService;
    private readonly ILogger<AtomicRefundPaymentHandler> _logger;

    public AtomicRefundPaymentHandler(
        IOrderRepository orderRepository,
        IPaymentTransactionRepository paymentRepo,
        IPaymentGatewayFactory gatewayFactory,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        PaymentSettlementService settlementService,
        ILogger<AtomicRefundPaymentHandler> logger)
    {
        _orderRepository = orderRepository;
        _paymentRepo = paymentRepo;
        _gatewayFactory = gatewayFactory;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _settlementService = settlementService;
        _logger = logger;
    }

    public async Task<AtomicRefundResult> Handle(
        AtomicRefundPaymentCommand request,
        CancellationToken ct
        )
    {
        // ─── Step 1: بارگذاری سفارش
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Fail($"سفارش {request.OrderId} یافت نشد.");

        // ─── Step 2: دریافت تراکنش پرداخت موفق
        var successfulPayment = await _paymentRepo.GetVerifiedByOrderIdAsync(request.OrderId, ct);
        if (successfulPayment is null)
            return Fail("تراکنش پرداخت موفق برای این سفارش یافت نشد.");

        // ─── Step 3: اعتبارسنجی امکان استرداد (Domain Service)
        var eligibility = _settlementService.ValidateRefundEligibility(order, successfulPayment);
        if (!eligibility.IsValid)
            return Fail(eligibility.Error!);

        // ─── Step 4: اعتبارسنجی مبلغ استرداد
        var amountValidation = _settlementService.ValidateRefundAmount(successfulPayment, request.PartialAmount);
        if (!amountValidation.IsValid)
            return Fail(amountValidation.Error!);

        var refundAmount = amountValidation.RefundAmount;

        _logger.LogInformation(
            "[Refund] Starting atomic refund for Order {OrderId}, Amount={Amount}",
            request.OrderId, refundAmount);

        // ─── Step 5: درخواست Refund از درگاه
        var gateway = _gatewayFactory.GetGateway(successfulPayment.Gateway);
        string? refundTransactionId = null;

        if (gateway is IRefundableGateway refundableGateway)
        {
            try
            {
                var refundResult = await refundableGateway.RefundAsync(
                    successfulPayment.RefId.ToString()!,
                    (int)refundAmount,
                    request.Reason);

                refundTransactionId = refundResult.RefundTransactionId;

                if (!refundResult.IsSuccess)
                {
                    _logger.LogError("[Refund] Gateway refund failed for Order {OrderId}: {Error}",
                        request.OrderId, refundResult.Message);
                    return Fail($"استرداد از درگاه ناموفق بود: {refundResult.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Refund] Gateway exception for Order {OrderId}", request.OrderId);
                return Fail("خطا در ارتباط با درگاه پرداخت برای استرداد.");
            }
        }
        else
        {
            refundTransactionId = $"MANUAL-REFUND-{Guid.NewGuid():N}";
            _logger.LogWarning(
                "[Refund] Gateway {Gateway} does not support automatic refund. Manual refund required.",
                gateway.GatewayName);
        }

        // ─── Step 6: اعمال استرداد روی هر دو Aggregate (Domain Service)
        var settlementResult = _settlementService.ProcessRefund(order, successfulPayment, request.Reason);
        if (!settlementResult.IsSuccess)
            return Fail(settlementResult.Error!);

        // ─── Step 7: برگشت موجودی
        await _inventoryService.ReturnStockForOrderAsync(
            order.Id,
            request.RequestedByUserId,
            request.Reason, ct);

        // ─── Step 8: ذخیره همه تغییرات در یک Transaction
        _paymentRepo.Update(successfulPayment);
        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[Refund] Order {OrderId} refunded successfully. Amount={Amount}, TxId={TxId}",
            request.OrderId, refundAmount, refundTransactionId);

        return new AtomicRefundResult(true, refundTransactionId, refundAmount, null);
    }

    private static AtomicRefundResult Fail(string error) =>
        new(false, null, null, error);
}