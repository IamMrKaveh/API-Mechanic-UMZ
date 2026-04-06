using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Presentation.Base.Controllers.v1;

namespace Presentation.Wallet.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWalletBalanceQuery(CurrentUser.UserId), ct);
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetWalletLedgerQuery(CurrentUser.UserId, page, pageSize), ct);
        return ToActionResult(result);
    }
}