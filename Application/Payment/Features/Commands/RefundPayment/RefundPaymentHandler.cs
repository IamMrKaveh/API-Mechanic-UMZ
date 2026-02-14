namespace Application.Payment.Features.Commands.RefundPayment;

public class RefundPaymentHandler : IRequestHandler<RefundPaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly PaymentDomainService _paymentDomainService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RefundPaymentHandler> _logger;

    public RefundPaymentHandler(
        IPaymentTransactionRepository repository,
        PaymentDomainService paymentDomainService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<RefundPaymentHandler> logger)
    {
        _repository = repository;
        _paymentDomainService = paymentDomainService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<PaymentResultDto>> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var tx = await _repository.GetByIdAsync(request.TransactionId, cancellationToken);
        if (tx == null)
        {
            return ServiceResult<PaymentResultDto>.Failure("تراکنش یافت نشد.");
        }

        // اعتبارسنجی امکان بازگشت با Domain Service
        var (canRefund, error) = _paymentDomainService.ValidateRefund(tx);
        if (!canRefund)
        {
            return ServiceResult<PaymentResultDto>.Failure(error!);
        }

        // اعمال استرداد از طریق متد دامین
        tx.Refund(request.Reason);
        _repository.Update(tx);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminEventAsync(
            "PaymentRefunded",
            request.AdminUserId,
            $"Transaction {tx.Id} refunded. RefId: {tx.RefId}, Amount: {tx.Amount.Amount:N0}, Reason: {request.Reason}");

        _logger.LogInformation("Payment {TransactionId} refunded by admin {AdminId}",
            request.TransactionId, request.AdminUserId);

        return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
        {
            IsSuccess = true,
            RefId = tx.RefId,
            Message = "استرداد وجه با موفقیت ثبت شد."
        });
    }
}