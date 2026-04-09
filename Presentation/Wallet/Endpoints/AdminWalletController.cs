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
public class AdminWalletController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{userId}/balance")]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWalletBalanceQuery(userId), ct);
        return ToActionResult(result);
    }

    [HttpGet("{userId}/ledger")]
    public async Task<IActionResult> GetLedger(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetWalletLedgerQuery(userId, page, pageSize), ct);
        return ToActionResult(result);
    }

    [HttpPost("{userId}/credit")]
    public async Task<IActionResult> Credit(
        Guid userId,
        [FromBody] AdminWalletAdjustmentRequest request,
        CancellationToken ct)
    {
        var adminId = CurrentUser.UserId;
        var correlationId = HttpContext.TraceIdentifier;

        var command = new CreditWalletCommand(
            UserId: userId,
            Amount: request.Amount,
            TransactionType: WalletTransactionType.Credit,
            ReferenceType: WalletReferenceType.Admin,
            ReferenceId: 0,
            IdempotencyKey: $"admin-credit-{userId}-{correlationId}",
            CorrelationId: correlationId,
            Description: BuildAuditDescription("CREDIT", adminId, request.Reason, request.Description));

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{userId}/debit")]
    public async Task<IActionResult> Debit(
        Guid userId,
        [FromBody] AdminWalletAdjustmentRequest request,
        CancellationToken ct)
    {
        var adminId = CurrentUser.UserId;
        var correlationId = HttpContext.TraceIdentifier;

        var command = new DebitWalletCommand(
            userId,
            request.Amount,
            WalletTransactionType.Debit,
            WalletReferenceType.Admin,
            ReferenceId: adminId,
            IdempotencyKey: $"admin-debit-{userId}-{correlationId}",
            CorrelationId: correlationId,
            Description: BuildAuditDescription("DEBIT", adminId, request.Reason, request.Description));

        var result = await _mediator.Send(command, ct);
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