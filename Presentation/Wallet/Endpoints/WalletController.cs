using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Shared;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/wallet")]
[Authorize]
public sealed class WalletController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("balance")]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        return await Send(new GetWalletBalanceQuery(RequestContext.UserId ?? Guid.Empty), ct);
    }

    [HttpGet("ledger")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletLedgerEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return await Send(new GetWalletLedgerQuery(RequestContext.UserId ?? Guid.Empty, page, pageSize), ct);
    }
}