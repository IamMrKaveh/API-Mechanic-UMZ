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

        var transactionResult = await (paymentQueryService.GetByAuthorityAsync(request.Authority, ct)).OrNotFoundAsync("تراکنش یافت نشد.");
        if (transactionResult.IsFailure) return transactionResult.Error;
        var transaction = transactionResult.Value;

        if (!currentUser.IsAdmin && transaction.UserId != currentUser.UserId.Value)
            return ServiceResult<PaymentStatusDto?>.Forbidden("دسترسی ممنوع.");

        var dtoResult = await (paymentQueryService.GetStatusByAuthorityAsync(request.Authority, ct)).OrNotFoundAsync("تراکنش یافت نشد.");
        if (dtoResult.IsFailure) return dtoResult.Error;
        var dto = dtoResult.Value;

        return ServiceResult<PaymentStatusDto?>.Success(dto);
    }
}
