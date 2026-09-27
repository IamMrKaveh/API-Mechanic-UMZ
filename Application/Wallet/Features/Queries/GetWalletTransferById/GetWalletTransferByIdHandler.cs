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
        return dto.ToResultOrNotFound("انتقال مورد نظر یافت نشد.");
    }
}
