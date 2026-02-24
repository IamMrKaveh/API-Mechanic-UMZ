namespace MainApi.Wallet.Controllers;

[ApiController]
[Route("api/admin/wallet")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin-wallet")]
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
        var result = await _mediator.Send(
            new Application.Wallet.Features.Queries.GetWalletBalance.GetWalletBalanceQuery(userId));
        return ToActionResult(result);
    }

    [HttpPost("{userId}/credit")]
    public async Task<IActionResult> Credit(int userId, [FromBody] AdminWalletAdjustmentDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var adminId = CurrentUser.UserId.Value;
        var correlationId = HttpContext.TraceIdentifier;

        var auditDescription = BuildAuditDescription("CREDIT", adminId, dto.Reason, dto.Description);

        var command = new Application.Wallet.Features.Commands.CreditWallet.CreditWalletCommand(
            UserId: userId,
            Amount: dto.Amount,
            TransactionType: WalletTransactionType.AdminAdjustmentCredit,
            ReferenceType: WalletReferenceType.Admin,
            ReferenceId: adminId,
            IdempotencyKey: $"admin-credit-{userId}-{correlationId}",
            CorrelationId: correlationId,
            Description: auditDescription);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("{userId}/debit")]
    public async Task<IActionResult> Debit(int userId, [FromBody] AdminWalletAdjustmentDto dto)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var adminId = CurrentUser.UserId.Value;
        var correlationId = HttpContext.TraceIdentifier;

        var auditDescription = BuildAuditDescription("DEBIT", adminId, dto.Reason, dto.Description);

        var command = new Application.Wallet.Features.Commands.DebitWallet.DebitWalletCommand(
            UserId: userId,
            Amount: dto.Amount,
            TransactionType: WalletTransactionType.AdminAdjustmentDebit,
            ReferenceType: WalletReferenceType.Admin,
            ReferenceId: adminId,
            IdempotencyKey: $"admin-debit-{userId}-{correlationId}",
            CorrelationId: correlationId,
            Description: auditDescription);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    private static string BuildAuditDescription(
        string operation,
        int adminId,
        string reason,
        string? extraNote)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"[ADMIN-{operation}] AdminId={adminId} | Reason={reason}");
        if (!string.IsNullOrWhiteSpace(extraNote))
            sb.Append($" | Note={extraNote}");
        return sb.ToString();
    }
}