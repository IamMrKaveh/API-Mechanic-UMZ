namespace Application.Payment.Features.Commands.ExpireStalePayments;

public class ExpireStalePaymentsHandler : IRequestHandler<ExpireStalePaymentsCommand, ServiceResult<int>>
{
    private readonly IPaymentTransactionRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly PaymentDomainService _paymentDomainService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpireStalePaymentsHandler> _logger;

    public ExpireStalePaymentsHandler(
        IPaymentTransactionRepository paymentRepository,
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        PaymentDomainService paymentDomainService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<ExpireStalePaymentsHandler> logger)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _paymentDomainService = paymentDomainService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> Handle(
        ExpireStalePaymentsCommand request,
        CancellationToken ct)
    {
        var staleTransactions = await _paymentRepository.GetPendingExpiredTransactionsAsync(
            request.CutoffTime, ct);

        var transactionList = staleTransactions.ToList();
        if (!transactionList.Any())
        {
            return ServiceResult<int>.Success(0);
        }

        
        var expiredCount = _paymentDomainService.ExpireStaleTransactions(transactionList);

        
        foreach (var tx in transactionList.Where(t => t.Status == PaymentStatus.Expired))
        {
            _paymentRepository.Update(tx);

            if (tx.Order != null && !tx.Order.IsPaid && tx.Order.CanBeCancelled())
            {
                tx.Order.Cancel(
                    cancelledBy: 0,
                    reason: $"لغو خودکار - پرداخت {tx.Authority} منقضی شد.");

                await _orderRepository.UpdateAsync(tx.Order, ct);

                
                foreach (var item in tx.Order.OrderItems)
                {
                    await _inventoryService.RollbackReservationAsync(
                        item.VariantId,
                        item.Quantity,
                        userId: null,
                        reason: $"System rollback for expired payment {tx.Authority}",
                        ct: ct);
                }
            }

            await _auditService.LogSystemEventAsync(
                "PaymentExpired",
                $"Transaction {tx.Authority} expired. OrderId: {tx.OrderId}");
        }

        if (expiredCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Expired {Count} stale payment transactions", expiredCount);
        return ServiceResult<int>.Success(expiredCount);
    }
}