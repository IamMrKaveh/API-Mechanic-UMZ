namespace MainApi.Wallet.Controllers;

[ApiController]
[Route("api/admin/wallet")]
[Authorize(Roles = "Admin")]
public class AdminWalletController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminWalletController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("{userId}/balance")]
    public async Task<IActionResult> GetBalance(int userId)
    {
        var result = await _mediator.Send(new Application.Wallet.Features.Queries.GetWalletBalance.GetWalletBalanceQuery(userId));
        return ToActionResult(result);
    }

    [HttpPost("{userId}/credit")]
    public async Task<IActionResult> Credit(int userId, [FromBody] AdminWalletAdjustmentDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new Application.Wallet.Features.Commands.CreditWallet.CreditWalletCommand(
            userId,
            dto.Amount,
            WalletTransactionType.AdminAdjustmentCredit,
            WalletReferenceType.Admin,
            CurrentUser.UserId.Value,
            $"admin-credit-{userId}-{Guid.NewGuid()}",
            Description: dto.Description);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{userId}/debit")]
    public async Task<IActionResult> Debit(int userId, [FromBody] AdminWalletAdjustmentDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new Application.Wallet.Features.Commands.DebitWallet.DebitWalletCommand(
            userId,
            dto.Amount,
            WalletTransactionType.AdminAdjustmentDebit,
            WalletReferenceType.Admin,
            CurrentUser.UserId.Value,
            $"admin-debit-{userId}-{Guid.NewGuid()}",
            Description: dto.Description);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}