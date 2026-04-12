using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.DebitWallet;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Domain.Wallet.Enums;
using Presentation.Wallet.Requests;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/admin/wallet")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin-wallet")]
public sealed class AdminWalletController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("{userId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetWalletBalanceQuery(userId), ct);
        return ToActionResult(result);
    }

    [HttpGet("{userId:guid}/ledger")]
    public async Task<IActionResult> GetLedger(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(new GetWalletLedgerQuery(userId, page, pageSize), ct);
        return ToActionResult(result);
    }

    [HttpPost("{userId:guid}/credit")]
    public async Task<IActionResult> Credit(
        Guid userId,
        [FromBody] AdminWalletAdjustmentRequest request,
        CancellationToken ct)
    {
        var command = new CreditWalletCommand(
            userId,
            request.Amount,
            WalletTransactionType.Credit,
            WalletReferenceType.Admin,
            "0",
            $"admin-credit-{userId}-{HttpContext.TraceIdentifier}",
            HttpContext.TraceIdentifier,
            BuildAuditDescription("CREDIT", CurrentUser.UserId, request.Reason, request.Description));

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{userId:guid}/debit")]
    public async Task<IActionResult> Debit(
        Guid userId,
        [FromBody] AdminWalletAdjustmentRequest request,
        CancellationToken ct)
    {
        var command = new DebitWalletCommand(
            userId,
            request.Amount,
            WalletTransactionType.Debit,
            WalletReferenceType.Admin,
            CurrentUser.UserId.ToString(),
            $"admin-debit-{userId}-{HttpContext.TraceIdentifier}",
            HttpContext.TraceIdentifier,
            BuildAuditDescription("DEBIT", CurrentUser.UserId, request.Reason, request.Description));

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    private static string BuildAuditDescription(
        string operation,
        Guid adminId,
        string reason,
        string? extraNote)
    {
        var sb = new StringBuilder();
        sb.Append($"[ADMIN-{operation}] AdminId={adminId} | Reason={reason}");
        if (!string.IsNullOrWhiteSpace(extraNote))
            sb.Append($" | Note={extraNote}");
        return sb.ToString();
    }
}