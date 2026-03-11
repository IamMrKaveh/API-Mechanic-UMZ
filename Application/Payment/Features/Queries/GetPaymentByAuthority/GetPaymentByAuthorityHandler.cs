using Application.Common.Models;

namespace Application.Payment.Features.Queries.GetPaymentByAuthority;

public class GetPaymentByAuthorityHandler
    : IRequestHandler<GetPaymentByAuthorityQuery, ServiceResult<PaymentTransactionDto?>>
{
    private readonly IPaymentQueryService _paymentQueryService;

    public GetPaymentByAuthorityHandler(IPaymentQueryService paymentQueryService)
    {
        _paymentQueryService = paymentQueryService;
    }

    public async Task<ServiceResult<PaymentTransactionDto?>> Handle(
        GetPaymentByAuthorityQuery request,
        CancellationToken ct)
    {
        var dto = await _paymentQueryService.GetByAuthorityAsync(request.Authority, ct);

        if (dto is null)
            return ServiceResult<PaymentTransactionDto?>.Failure("تراکنش یافت نشد.");

        return ServiceResult<PaymentTransactionDto?>.Success(dto);
    }
}