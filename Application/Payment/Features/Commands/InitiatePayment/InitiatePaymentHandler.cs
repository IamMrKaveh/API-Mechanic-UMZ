using Application.Common.Results;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Common.Exceptions;

namespace Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandler(IPaymentService paymentService) : IRequestHandler<InitiatePaymentCommand, ServiceResult<PaymentResultDto>>
{
    private readonly IPaymentService _paymentService = paymentService;

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

            return ServiceResult<PaymentResultDto>.Unexpected(result.Error?.Message ?? "Failed to initiate payment");
        }
        catch (DomainException ex)
        {
            return ServiceResult<PaymentResultDto>.Unexpected(ex.Message);
        }
    }
}