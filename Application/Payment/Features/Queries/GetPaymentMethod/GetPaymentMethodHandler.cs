using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Payment.ValueObjects;

namespace Application.Payment.Features.Queries.GetPaymentMethod;

public sealed class GetPaymentMethodHandler(
    IPaymentMethodQueryService queryService)
    : IQueryHandler<GetPaymentMethodQuery, PaymentMethodDto>
{
    public async Task<ServiceResult<PaymentMethodDto>> Handle(
        GetPaymentMethodQuery request,
        CancellationToken ct)
    {
        var id = PaymentMethodId.From(request.Id);
        var dto = await queryService.GetByIdAsync(id, ct);

        return dto is null
            ? ServiceResult<PaymentMethodDto>.NotFound("روش پرداخت یافت نشد.")
            : ServiceResult<PaymentMethodDto>.Success(dto);
    }
}