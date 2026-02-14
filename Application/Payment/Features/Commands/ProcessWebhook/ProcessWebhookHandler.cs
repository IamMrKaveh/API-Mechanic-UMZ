namespace Application.Payment.Features.Commands.ProcessWebhook;

public class ProcessWebhookHandler : IRequestHandler<ProcessWebhookCommand, ServiceResult>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly PaymentDomainService _paymentDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessWebhookHandler> _logger;

    public ProcessWebhookHandler(
        IPaymentTransactionRepository repository,
        PaymentDomainService paymentDomainService,
        IUnitOfWork unitOfWork,
        ILogger<ProcessWebhookHandler> logger)
    {
        _repository = repository;
        _paymentDomainService = paymentDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> Handle(ProcessWebhookCommand request, CancellationToken cancellationToken)
    {
        var tx = await _repository.GetByAuthorityWithOrderAsync(request.Authority, cancellationToken);
        if (tx == null)
        {
            _logger.LogWarning("Webhook: Transaction not found {Authority}", request.Authority);
            return ServiceResult.Failure("تراکنش یافت نشد.");
        }

        // بررسی تکراری بودن
        if (tx.IsSuccessful())
        {
            _logger.LogInformation("Webhook: Transaction {Authority} already succeeded", request.Authority);
            return ServiceResult.Success();
        }

        if (request.Status.Equals("OK", StringComparison.OrdinalIgnoreCase) && request.RefId.HasValue)
        {
            // استفاده از Domain Service برای پردازش نتیجه موفق
            if (tx.Order != null)
            {
                var processResult = _paymentDomainService.ProcessSuccessfulPayment(
                    tx, tx.Order, request.RefId.Value,
                    rawResponse: $"Webhook: {request.Status}");

                if (!processResult.IsSuccess)
                {
                    _logger.LogWarning("Webhook: Processing failed for {Authority}: {Error}",
                        request.Authority, processResult.Error);
                    return ServiceResult.Failure(processResult.Error!);
                }
            }
            else
            {
                // اگر Order لود نشده فقط تراکنش ر�� موفق علامت بزن
                tx.MarkAsSuccess(request.RefId.Value, rawResponse: $"Webhook: {request.Status}");
            }
        }
        else
        {
            _paymentDomainService.ProcessFailedPayment(tx, $"Webhook Failed: {request.Status}");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}