namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandler : IRequestHandler<VerifyPaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentTransactionRepository _repository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly PaymentDomainService _paymentDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;
    private readonly ILogger<VerifyPaymentHandler> _logger;

    public VerifyPaymentHandler(
        IPaymentTransactionRepository repository,
        IPaymentGateway paymentGateway,
        PaymentDomainService paymentDomainService,
        IUnitOfWork unitOfWork,
        IOptions<FrontendUrlsDto> frontendUrls,
        ILogger<VerifyPaymentHandler> logger)
    {
        _repository = repository;
        _paymentGateway = paymentGateway;
        _paymentDomainService = paymentDomainService;
        _unitOfWork = unitOfWork;
        _frontendUrls = frontendUrls;
        _logger = logger;
    }

    public async Task<ServiceResult<PaymentResultDto>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var tx = await _repository.GetByAuthorityWithOrderAsync(request.Authority, cancellationToken);
        if (tx == null)
        {
            return ServiceResult<PaymentResultDto>.Failure("تراکنش یافت نشد.");
        }

        var baseUrl = _frontendUrls.Value.BaseUrl;

        // 1. اعتبارسنجی با Domain Service
        var validation = _paymentDomainService.ValidateVerification(tx, request.Status);

        if (validation.IsAlreadyVerified)
        {
            return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
            {
                IsSuccess = true,
                RefId = validation.ExistingRefId,
                Message = "تراکنش قبلاً تأیید شده است.",
                RedirectUrl = $"{baseUrl}/payment/result?status=success&refId={validation.ExistingRefId}&orderId={tx.OrderId}"
            });
        }

        if (validation.IsUserCancelled)
        {
            _paymentDomainService.ProcessFailedPayment(tx, "لغو توسط کاربر یا خطای درگاه.");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
            {
                IsSuccess = false,
                Message = "پرداخت لغو شد.",
                RedirectUrl = $"{baseUrl}/payment/result?status=failure&reason=cancelled"
            });
        }

        if (!validation.IsValid)
        {
            return ServiceResult<PaymentResultDto>.Failure(validation.Error ?? "خطای اعتبارسنجی.");
        }

        // 2. تغییر وضعیت به Processing (جلوگیری از Race Condition)
        tx.MarkAsVerificationInProgress();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. تأیید با درگاه
        try
        {
            var verification = await _paymentGateway.VerifyPaymentAsync(request.Authority, (int)tx.Amount.Amount);

            if (verification.IsVerified && verification.RefId.HasValue)
            {
                // استفاده از Domain Service برای پردازش نتیجه موفق
                var processResult = _paymentDomainService.ProcessSuccessfulPayment(
                    tx, tx.Order!, verification.RefId.Value,
                    verification.CardPan, verification.CardHash,
                    verification.Fee, verification.RawResponse);

                if (!processResult.IsSuccess)
                {
                    return ServiceResult<PaymentResultDto>.Failure(processResult.Error!);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
                {
                    IsSuccess = true,
                    RefId = verification.RefId,
                    Message = "پرداخت با موفقیت انجام شد.",
                    RedirectUrl = $"{baseUrl}/payment/result?status=success&refId={verification.RefId}&orderId={tx.OrderId}"
                });
            }
            else
            {
                _paymentDomainService.ProcessFailedPayment(tx, verification.Message, verification.RawResponse);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
                {
                    IsSuccess = false,
                    Message = verification.Message,
                    RedirectUrl = $"{baseUrl}/payment/result?status=failure&reason={Uri.EscapeDataString(verification.Message ?? "Error")}"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment {Authority}", request.Authority);
            return ServiceResult<PaymentResultDto>.Failure("خطای سیستمی در تأیید پرداخت.");
        }
    }
}