using Application.Common.Results;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentStatus;

public class GetPaymentStatusHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetPaymentStatusQuery, ServiceResult<PaymentStatusDto?>>
{
    private readonly IPaymentQueryService _paymentQueryService = paymentQueryService;

    public async Task<ServiceResult<PaymentStatusDto?>> Handle(
        GetPaymentStatusQuery request,
        CancellationToken ct)
    {
        var dto = await _paymentQueryService.GetStatusByAuthorityAsync(request.Authority, ct);

        if (dto is null)
            return ServiceResult<PaymentStatusDto?>.NotFound("تراکنش یافت نشد.");

        return ServiceResult<PaymentStatusDto?>.Success(dto);
    }
}