namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandler : IRequestHandler<VerifyPaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentService _paymentService;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;
    private readonly ILogger<VerifyPaymentHandler> _logger;

    public VerifyPaymentHandler(
        IPaymentService paymentService,
        IOptions<FrontendUrlsDto> frontendUrls,
        ILogger<VerifyPaymentHandler> logger)
    {
        _paymentService = paymentService;
        _frontendUrls = frontendUrls;
        _logger = logger;
    }

    public async Task<ServiceResult<PaymentResultDto>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var baseUrl = _frontendUrls.Value.BaseUrl;

        
        
        

        var result = await _paymentService.VerifyPaymentAsync(request.Authority, 0, cancellationToken); 

        if (result.IsSucceed && result.Data != default)
        {
            var data = result.Data;
            var redirectUrl = data.IsVerified
                ? $"{baseUrl}/payment/result?status=success&refId={data.RefId}"
                : $"{baseUrl}/payment/result?status=failure&reason={Uri.EscapeDataString(data.Message ?? "Error")}";

            return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
            {
                IsSuccess = data.IsVerified,
                RefId = data.RefId,
                Message = data.Message,
                RedirectUrl = redirectUrl
            });
        }

        return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
        {
            IsSuccess = false,
            Message = result.Error,
            RedirectUrl = $"{baseUrl}/payment/result?status=failure&reason={Uri.EscapeDataString(result.Error ?? "Error")}"
        });
    }
}