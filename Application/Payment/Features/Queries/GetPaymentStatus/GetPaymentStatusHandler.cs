using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentStatus;

public class GetPaymentStatusHandler(
    IPaymentQueryService paymentQueryService,
    ICurrentUserService currentUser)
    : IQueryHandler<GetPaymentStatusQuery, PaymentStatusDto?>
{
    public async Task<ServiceResult<PaymentStatusDto?>> Handle(
        GetPaymentStatusQuery request,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return ServiceResult<PaymentStatusDto?>.Unauthorized("کاربر احراز هویت نشده است.");

        var transaction = await paymentQueryService.GetByAuthorityAsync(request.Authority, ct);
        if (transaction is null)
            return ServiceResult<PaymentStatusDto?>.NotFound("تراکنش یافت نشد.");

        if (!currentUser.IsAdmin && transaction.UserId != currentUser.UserId.Value)
            return ServiceResult<PaymentStatusDto?>.Forbidden("دسترسی ممنوع.");

        var dto = await paymentQueryService.GetStatusByAuthorityAsync(request.Authority, ct);
        if (dto is null)
            return ServiceResult<PaymentStatusDto?>.NotFound("تراکنش یافت نشد.");

        return ServiceResult<PaymentStatusDto?>.Success(dto);
    }
}
