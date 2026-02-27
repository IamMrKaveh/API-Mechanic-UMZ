namespace MainApi.Wallet.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : BaseApiController
{
    private readonly IMediator _mediator;

    public WalletController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var result = await _mediator.Send(new GetWalletBalanceQuery(CurrentUser.UserId.Value), ct);
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var result = await _mediator.Send(
            new GetWalletLedgerQuery(CurrentUser.UserId.Value, page, pageSize), ct);
        return ToActionResult(result);
    }
}