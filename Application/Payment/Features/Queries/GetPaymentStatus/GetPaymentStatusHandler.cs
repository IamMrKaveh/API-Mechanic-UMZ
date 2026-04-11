using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentStatus;

public class GetPaymentStatusHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetPaymentStatusQuery, ServiceResult<PaymentStatusDto?>>
{
    public async Task<ServiceResult<PaymentStatusDto?>> Handle(
        GetPaymentStatusQuery request,
        CancellationToken ct)
    {
        var dto = await paymentQueryService.GetStatusByAuthorityAsync(request.Authority, ct);

        if (dto is null)
            return ServiceResult<PaymentStatusDto?>.NotFound("تراکنش یافت نشد.");

        return ServiceResult<PaymentStatusDto?>.Success(dto);
    }
}