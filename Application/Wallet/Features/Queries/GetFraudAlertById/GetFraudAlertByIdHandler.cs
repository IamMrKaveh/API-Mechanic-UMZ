using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetFraudAlertById;

public sealed class GetFraudAlertByIdHandler(IWalletFraudAlertQueryService queryService)
    : IRequestHandler<GetFraudAlertByIdQuery, ServiceResult<WalletFraudAlertDto>>
{
    public async Task<ServiceResult<WalletFraudAlertDto>> Handle(
        GetFraudAlertByIdQuery request,
        CancellationToken ct)
    {
        var dto = await queryService.GetByIdAsync(request.AlertId, ct);
        return dto is null
            ? ServiceResult<WalletFraudAlertDto>.NotFound("هشدار مورد نظر یافت نشد.")
            : ServiceResult<WalletFraudAlertDto>.Success(dto);
    }
}