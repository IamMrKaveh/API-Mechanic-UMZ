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
    public async Task<IActionResult> GetBalance()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var result = await _mediator.Send(new Application.Wallet.Features.Queries.GetWalletBalance.GetWalletBalanceQuery(CurrentUser.UserId.Value));
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var result = await _mediator.Send(new Application.Wallet.Features.Queries.GetWalletLedger.GetWalletLedgerQuery(CurrentUser.UserId.Value, page, pageSize));
        return ToActionResult(result);
    }
}