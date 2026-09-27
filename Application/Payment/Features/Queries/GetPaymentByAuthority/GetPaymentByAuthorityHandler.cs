using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Queries.GetPaymentByAuthority;

public class GetPaymentByAuthorityHandler(
    IPaymentQueryService paymentQueryService,
    ICurrentUserService currentUser)
    : IQueryHandler<GetPaymentByAuthorityQuery, PaymentTransactionDto?>
{
    public async Task<ServiceResult<PaymentTransactionDto?>> Handle(
        GetPaymentByAuthorityQuery request,
        CancellationToken ct)
    {
        if (!currentUser.UserId.HasValue)
            return ServiceResult<PaymentTransactionDto?>.Unauthorized("کاربر احراز هویت نشده است.");

        var dtoResult = await (paymentQueryService.GetByAuthorityAsync(request.Authority, ct)).OrNotFoundAsync("تراکنش یافت نشد.");
        if (dtoResult.IsFailure) return dtoResult.Error;
        var dto = dtoResult.Value;

        if (!currentUser.IsAdmin && dto.UserId != currentUser.UserId.Value)
            return ServiceResult<PaymentTransactionDto?>.Forbidden("دسترسی ممنوع.");

        return ServiceResult<PaymentTransactionDto?>.Success(dto);
    }
}
