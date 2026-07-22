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

        var dto = await paymentQueryService.GetByAuthorityAsync(request.Authority, ct);

        if (dto is null)
            return ServiceResult<PaymentTransactionDto?>.NotFound("تراکنش یافت نشد.");

        if (!currentUser.IsAdmin && dto.UserId != currentUser.UserId.Value)
            return ServiceResult<PaymentTransactionDto?>.Forbidden("دسترسی ممنوع.");

        return ServiceResult<PaymentTransactionDto?>.Success(dto);
    }
}
