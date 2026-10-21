using Application.Common.Interfaces;
﻿using Application.Wallet.Features.Shared;

namespace Application.Wallet.Features.Queries.GetFraudAlertById;

public sealed class GetFraudAlertByIdHandler(IWalletFraudAlertQueryService queryService)
    : IQueryHandler<GetFraudAlertByIdQuery, WalletFraudAlertDto>
{
    public async Task<ServiceResult<WalletFraudAlertDto>> Handle(
        GetFraudAlertByIdQuery request,
        CancellationToken ct)
    {
        var dto = await queryService.GetByIdAsync(request.AlertId, ct);
        return dto.ToResultOrNotFound("هشدار مورد نظر یافت نشد.");
    }
}