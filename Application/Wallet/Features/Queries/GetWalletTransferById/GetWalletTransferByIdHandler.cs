using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetWalletTransferById;

public sealed class GetWalletTransferByIdHandler(IWalletTransferQueryService queryService)
    : IQueryHandler<GetWalletTransferByIdQuery, WalletTransferDto>
{
    public async Task<ServiceResult<WalletTransferDto>> Handle(
        GetWalletTransferByIdQuery request,
        CancellationToken ct)
    {
        var dto = await queryService.GetByIdAsync(request.Id, ct);
        return dto is null
            ? ServiceResult<WalletTransferDto>.NotFound("انتقال مورد نظر یافت نشد.")
            : ServiceResult<WalletTransferDto>.Success(dto);
    }
}
