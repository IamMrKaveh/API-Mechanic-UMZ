namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentGateway _paymentGateway;
    private readonly IPaymentTransactionRepository _repository;
    private readonly IOrderRepository _orderRepository;
    private readonly PaymentDomainService _paymentDomainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InitiatePaymentHandler> _logger;

    public InitiatePaymentHandler(
        IPaymentGateway paymentGateway,
        IPaymentTransactionRepository repository,
        IOrderRepository orderRepository,
        PaymentDomainService paymentDomainService,
        IUnitOfWork unitOfWork,
        ILogger<InitiatePaymentHandler> logger)
    {
        _paymentGateway = paymentGateway;
        _repository = repository;
        _orderRepository = orderRepository;
        _paymentDomainService = paymentDomainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<PaymentResultDto>> Handle(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. بررسی وجود پرداخت در انتظار برای سفارش
        var hasPending = await _repository.HasPendingPaymentAsync(request.Dto.OrderId, cancellationToken);
        if (hasPending)
        {
            return ServiceResult<PaymentResultDto>.Failure("یک پرداخت در انتظار برای این سفارش وجود دارد.");
        }

        // 2. بررسی پرداخت قبلی موفق
        var hasSuccessful = await _repository.HasSuccessfulPaymentAsync(request.Dto.OrderId, cancellationToken);
        if (hasSuccessful)
        {
            return ServiceResult<PaymentResultDto>.Failure("این سفارش قبلاً پرداخت شده است.");
        }

        // 3. درخواست پرداخت از درگاه
        var result = await _paymentGateway.RequestPaymentAsync(
            request.Dto.Amount,
            request.Dto.Description,
            request.Dto.CallbackUrl,
            request.Dto.Mobile,
            request.Dto.Email);

        if (!result.IsSuccess || string.IsNullOrEmpty(result.Authority))
        {
            _logger.LogError("Payment gateway request failed: {Message}", result.Message);
            return ServiceResult<PaymentResultDto>.Failure(result.Message ?? "خطا در برقراری ارتباط با درگاه پرداخت.");
        }

        // 4. ایجاد تراکنش از طریق Factory Method دامین
        var transaction = PaymentTransaction.Initiate(
            request.Dto.OrderId,
            request.UserId,
            result.Authority,
            request.Dto.Amount,
            _paymentGateway.GatewayName,
            request.Dto.Description,
            request.IpAddress,
            result.RawResponse
        );

        await _repository.AddAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
        {
            IsSuccess = true,
            PaymentUrl = result.PaymentUrl,
            Authority = result.Authority
        });
    }
}