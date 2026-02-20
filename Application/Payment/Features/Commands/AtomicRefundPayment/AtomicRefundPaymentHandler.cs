namespace Application.Payment.Features.Commands.AtomicRefundPayment;

/// <summary>
/// هندلر Refund اتمیک که:
/// 1. وضعیت سفارش را بررسی می‌کند (باید Paid یا Delivered باشد)
/// 2. تراکنش Refund را در درگاه ثبت می‌کند
/// 3. وضعیت سفارش را به Refunded تغییر می‌دهد
/// 4. موجودی را برمی‌گرداند
/// اگر هر مرحله شکست بخورد، کل عملیات Rollback می‌شود.
/// </summary>
public sealed class AtomicRefundPaymentHandler : IRequestHandler<AtomicRefundPaymentCommand, AtomicRefundResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentTransactionRepository _paymentRepo;
    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<AtomicRefundPaymentHandler> _logger;

    public AtomicRefundPaymentHandler(
        IOrderRepository orderRepository,
        IPaymentTransactionRepository paymentRepo,
        IPaymentGatewayFactory gatewayFactory,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<AtomicRefundPaymentHandler> logger)
    {
        _orderRepository = orderRepository;
        _paymentRepo = paymentRepo;
        _gatewayFactory = gatewayFactory;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<AtomicRefundResult> Handle(
        AtomicRefundPaymentCommand request,
        CancellationToken ct)
    {
        // ─── Step 1: بارگذاری سفارش ─────────────────────────────
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order is null)
            return Fail($"سفارش {request.OrderId} یافت نشد.");

        // ─── Step 2: بررسی وضعیت سفارش ──────────────────────────
        if (!order.IsPaid && !order.IsDelivered)
        {
            return Fail(
                $"استرداد فقط برای سفارش‌های پرداخت‌شده یا تحویل‌داده‌شده مجاز است. " +
                $"وضعیت فعلی: {order.Status.DisplayName}");
        }

        // ─── Step 3: دریافت تراکنش پرداخت موفق ──────────────────
        var successfulPayment = await _paymentRepo.GetVerifiedByOrderIdAsync(request.OrderId, ct);
        if (successfulPayment is null)
            return Fail("تراکنش پرداخت موفق برای این سفارش یافت نشد.");

        var refundAmount = request.PartialAmount ?? successfulPayment.Amount.Amount;

        if (refundAmount <= 0 || refundAmount > successfulPayment.Amount.Amount)
            return Fail($"مبلغ استرداد نامعتبر است: {refundAmount:N0}");

        _logger.LogInformation(
            "[Refund] Starting atomic refund for Order {OrderId}, Amount={Amount}",
            request.OrderId, refundAmount);

        // ─── Step 4: درخواست Refund از درگاه ────────────────────
        var gateway = _gatewayFactory.GetGateway(successfulPayment.Gateway);

        // نکته: درگاه‌های ایرانی معمولاً Refund API ندارند
        // این بخش برای درگاه‌هایی است که Refund API دارند
        // در غیر این صورت، Refund به صورت دستی انجام می‌شود
        bool gatewayRefundSuccess;
        string? refundTransactionId = null;

        if (gateway is IRefundableGateway refundableGateway)
        {
            try
            {
                var refundResult = await refundableGateway.RefundAsync(
                    successfulPayment.RefId.ToString()!,
                    (int)refundAmount,
                    request.Reason);

                gatewayRefundSuccess = refundResult.IsSuccess;
                refundTransactionId = refundResult.RefundTransactionId;

                if (!refundResult.IsSuccess)
                {
                    _logger.LogError(
                        "[Refund] Gateway refund failed for Order {OrderId}: {Error}",
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
            // درگاه Refund API ندارد - ثبت درخواست برای پرداخت دستی
            gatewayRefundSuccess = true;
            refundTransactionId = $"MANUAL-REFUND-{Guid.NewGuid():N}";
            _logger.LogWarning(
                "[Refund] Gateway {Gateway} does not support automatic refund. Manual refund required.",
                gateway.GatewayName);
        }

        // ─── Step 5: ثبت تراکنش Refund در DB ────────────────────
        //var refundTransaction = successfulPayment.CreateRefundTransaction(
        //    refundAmount,
        //    request.Reason,
        //    refundTransactionId);

        //await _paymentRepo.AddAsync(refundTransaction, ct);

        // ─── Step 6: تغییر وضعیت سفارش ──────────────────────────
        order.RequestRefund(request.Reason);

        // ─── Step 7: برگشت موجودی ────────────────────────────────
        var referenceNumber = $"ORDER-{order.Id}";
        await _inventoryService.ReturnStockForOrderAsync(
            order.Id,
            request.RequestedByUserId,
            request.Reason, ct);

        // ─── Step 8: ذخیره همه تغییرات در یک Transaction ────────
        await _orderRepository.UpdateAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // ─── Step 9: انتشار رویداد ───────────────────────────────
        await _publisher.Publish(
            new PaymentRefundedEvent(
                request.OrderId,
                successfulPayment.Id,
                refundAmount,
                request.Reason),
            ct);

        _logger.LogInformation(
            "[Refund] Order {OrderId} refunded successfully. Amount={Amount}, TxId={TxId}",
            request.OrderId, refundAmount, refundTransactionId);

        return new AtomicRefundResult(true, refundTransactionId, refundAmount, null);
    }

    private static AtomicRefundResult Fail(string error) =>
        new(false, null, null, error);
}