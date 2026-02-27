using Application.Wallet.Contracts;

namespace MainApi.Wallet.Controllers;

[ApiController]
[Route("api/admin/wallet")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin-wallet")]
public class AdminWalletController : BaseApiController
{
    private readonly IWalletService _walletService;

    public AdminWalletController(IWalletService walletService, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _walletService = walletService;
    }

    [HttpGet("{userId}/balance")]
    public async Task<IActionResult> GetBalance(int userId, CancellationToken ct)
    {
        var result = await _walletService.GetBalanceAsync(userId, ct);
        return ToActionResult(result);
    }

    [HttpPost("{userId}/credit")]
    public async Task<IActionResult> Credit(
        int userId,
        [FromBody] AdminWalletAdjustmentDto dto,
        CancellationToken ct)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var adminId = CurrentUser.UserId.Value;
        var correlationId = HttpContext.TraceIdentifier;
        var auditDescription = BuildAuditDescription("CREDIT", adminId, dto.Reason, dto.Description);

        var result = await _walletService.CreditAsync(
            userId,
            dto.Amount,
            WalletTransactionType.AdminAdjustmentCredit,
            WalletReferenceType.Admin,
            adminId,
            idempotencyKey: $"admin-credit-{userId}-{correlationId}",
            correlationId: correlationId,
            description: auditDescription,
            ct: ct);

        return ToActionResult(result);
    }

    [HttpPost("{userId}/debit")]
    public async Task<IActionResult> Debit(
        int userId,
        [FromBody] AdminWalletAdjustmentDto dto,
        CancellationToken ct)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var adminId = CurrentUser.UserId.Value;
        var correlationId = HttpContext.TraceIdentifier;
        var auditDescription = BuildAuditDescription("DEBIT", adminId, dto.Reason, dto.Description);

        var result = await _walletService.DebitAsync(
            userId,
            dto.Amount,
            WalletTransactionType.AdminAdjustmentDebit,
            WalletReferenceType.Admin,
            adminId,
            idempotencyKey: $"admin-debit-{userId}-{correlationId}",
            correlationId: correlationId,
            description: auditDescription,
            ct: ct);

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