using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWithdrawalById;

public sealed class GetWithdrawalByIdHandler(
    IWalletWithdrawalQueryService queryService)
    : IQueryHandler<GetWithdrawalByIdQuery, WalletWithdrawalRequestDto>
{
    public async Task<ServiceResult<WalletWithdrawalRequestDto>> Handle(
        GetWithdrawalByIdQuery request,
        CancellationToken ct)
    {
        var dto = await queryService.GetByIdAsync(request.WithdrawalId, ct);
        if (dto is null)
            return ServiceResult<WalletWithdrawalRequestDto>.NotFound("درخواست برداشت یافت نشد.");

        if (!request.IsAdmin)
        {
            if (request.RequesterUserId is null || dto.UserId != request.RequesterUserId.Value)
                return ServiceResult<WalletWithdrawalRequestDto>.Forbidden(
                    "شما مجاز به مشاهده این درخواست نیستید.");
        }

        return ServiceResult<WalletWithdrawalRequestDto>.Success(dto);
    }
}