using Application.Wallet.Contracts;

namespace MainApi.Wallet.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : BaseApiController
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _walletService = walletService;
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var result = await _walletService.GetBalanceAsync(CurrentUser.UserId.Value, ct);
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var result = await _walletService.GetLedgerAsync(
            CurrentUser.UserId.Value, page, pageSize, ct);
        return ToActionResult(result);
    }
}