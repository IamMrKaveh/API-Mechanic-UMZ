using Application.Common.Features.Shared;
using Application.Common.Results;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Common.Exceptions;

namespace Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandler(
    IPaymentService paymentService,
    IOptions<FrontendUrlsDto> frontendUrls,
    ILogger<VerifyPaymentHandler> logger) : IRequestHandler<VerifyPaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentService _paymentService = paymentService;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls = frontendUrls;
    private readonly ILogger<VerifyPaymentHandler> _logger = logger;

    public async Task<ServiceResult<PaymentResultDto>> Handle(VerifyPaymentCommand request, CancellationToken cancellationToken)
    {
        var baseUrl = _frontendUrls.Value.BaseUrl;

        try
        {
            var result = await _paymentService.VerifyPaymentAsync(request.Authority, 0, cancellationToken);

            if (result.IsSuccess && result.Value != default)
            {
                var data = result.Value;
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
                Message = result.Error?.Message,
                RedirectUrl = $"{baseUrl}/payment/result?status=failure&reason={Uri.EscapeDataString(result.Error ?? "Error")}"
            });
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("VerifyPayment: Domain exception for {Authority}: {Message}", request.Authority, ex.Message);
            return ServiceResult<PaymentResultDto>.Unexpected(ex.Message);
        }
    }
}