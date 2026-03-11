using Application.Common.Models;

namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandler : IRequestHandler<InitiatePaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentService _paymentService;

    public InitiatePaymentHandler(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public async Task<ServiceResult<PaymentResultDto>> Handle(
        InitiatePaymentCommand request,
        CancellationToken ct)
    {
        try
        {
            var result = await _paymentService.InitiatePaymentAsync(request.Dto, ct);

            if (result.IsSuccess && result.Value != default)
            {
                return ServiceResult<PaymentResultDto>.Success(new PaymentResultDto
                {
                    IsSuccess = result.Value.IsSuccess,
                    Authority = result.Value.Authority,
                    PaymentUrl = result.Value.PaymentUrl,
                    Message = result.Value.Message
                });
            }

            return ServiceResult<PaymentResultDto>.Failure(result.Error ?? "Failed to initiate payment", result.StatusCode);
        }
        catch (DomainException ex)
        {
            return ServiceResult<PaymentResultDto>.Failure(ex.Message);
        }
    }
}