using Application.Common.Models;

namespace Application.Payment.Features.Queries.GetPaymentStatus;

public class GetPaymentStatusHandler
    : IRequestHandler<GetPaymentStatusQuery, ServiceResult<PaymentStatusDto>>
{
    private readonly IPaymentQueryService _paymentQueryService;

    public GetPaymentStatusHandler(IPaymentQueryService paymentQueryService)
    {
        _paymentQueryService = paymentQueryService;
    }

    public async Task<ServiceResult<PaymentStatusDto>> Handle(
        GetPaymentStatusQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await _paymentQueryService.GetStatusByAuthorityAsync(request.Authority, cancellationToken);

        if (dto is null)
            return ServiceResult<PaymentStatusDto>.Failure("تراکنش یافت نشد.");

        return ServiceResult<PaymentStatusDto>.Success(dto);
    }
}