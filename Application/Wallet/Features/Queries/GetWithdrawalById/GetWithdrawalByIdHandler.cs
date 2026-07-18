using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWithdrawalById;

public sealed class GetWithdrawalByIdHandler(
    IWalletWithdrawalQueryService queryService,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetWithdrawalByIdQuery, WalletWithdrawalRequestDto>
{
    public async Task<ServiceResult<WalletWithdrawalRequestDto>> Handle(
        GetWithdrawalByIdQuery request,
        CancellationToken ct)
    {
        var dto = await queryService.GetByIdAsync(request.Id, ct);
        if (dto is null)
            return ServiceResult<WalletWithdrawalRequestDto>.NotFound("درخواست برداشت یافت نشد.");

        if (currentUserService.IsAdmin is false)
        {
            if (currentUserService.UserId is null || dto.UserId != currentUserService.UserId.Value)
                return ServiceResult<WalletWithdrawalRequestDto>.Forbidden(
                    "شما مجاز به مشاهده این درخواست نیستید.");
        }

        return ServiceResult<WalletWithdrawalRequestDto>.Success(dto);
    }
}