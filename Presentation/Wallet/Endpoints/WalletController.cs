using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/wallet")]
[Authorize]
public sealed class WalletController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetWalletBalanceQuery(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(
            new GetWalletLedgerQuery(CurrentUser.UserId, page, pageSize), ct);
        return ToActionResult(result);
    }
}