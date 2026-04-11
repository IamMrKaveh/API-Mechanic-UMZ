using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentByAuthority;

public class GetPaymentByAuthorityHandler(IPaymentQueryService paymentQueryService)
        : IRequestHandler<GetPaymentByAuthorityQuery, ServiceResult<PaymentTransactionDto?>>
{
    public async Task<ServiceResult<PaymentTransactionDto?>> Handle(
        GetPaymentByAuthorityQuery request,
        CancellationToken ct)
    {
        var dto = await paymentQueryService.GetByAuthorityAsync(request.Authority, ct);

        if (dto is null)
            return ServiceResult<PaymentTransactionDto?>.NotFound("تراکنش یافت نشد.");

        return ServiceResult<PaymentTransactionDto?>.Success(dto);
    }
}