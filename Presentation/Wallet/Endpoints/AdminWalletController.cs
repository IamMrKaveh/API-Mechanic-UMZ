using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.DismissFraudAlert;
using Application.Wallet.Features.Commands.FreezeWallet;
using Application.Wallet.Features.Commands.MarkFraudAlertReviewed;
using Application.Wallet.Features.Commands.RequestWalletDebit;
using Application.Wallet.Features.Commands.UnfreezeWallet;
using Application.Wallet.Features.Queries.ExportWalletLedger;
using Application.Wallet.Features.Queries.GetFraudAlertById;
using Application.Wallet.Features.Queries.GetFraudAlerts;
using Application.Wallet.Features.Queries.GetOpenFraudAlertsCount;
using Application.Wallet.Features.Queries.GetPendingDebitRequestsByUser;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Queries.GetWalletsOverview;
using Application.Wallet.Features.Queries.GetWalletStatistics;
using Domain.Wallet.Enums;
using Presentation.Wallet.Requests;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/wallets")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin-wallet")]
public sealed class AdminWalletController(IMediator mediator) : BaseApiController(mediator)
{
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private const int IdempotencyKeyMaxLength = 128;

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview([FromQuery] GetWalletsOverviewRequest request, CancellationToken ct)
    {
        var query = new GetWalletsOverviewQuery(
            request.Search, request.IsFrozen, request.MinBalance, request.MaxBalance,
            request.CreatedFrom, request.CreatedTo, request.SortBy, request.Page, request.PageSize);
        return await Send(query, ct);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics(CancellationToken ct)
        => await Send(new GetWalletStatisticsQuery(), ct);

    [HttpGet("{userId:guid}/balance")]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
        => await Send(new GetWalletBalanceQuery(userId), ct);

    [HttpGet("{userId:guid}/ledger")]
    public async Task<IActionResult> GetLedger(
        Guid userId,
        [FromQuery] GetAdminWalletLedgerRequest request,
        CancellationToken ct = default)
    {
        var query = new GetWalletLedgerQuery(
            userId, request.Page, request.PageSize, request.FromDate, request.ToDate,
            request.TransactionType, request.MinAmount, request.MaxAmount, request.SearchTerm);
        return await Send(query, ct);
    }

    [HttpGet("{userId:guid}/ledger/export")]
    public async Task<IActionResult> ExportLedger(
        Guid userId,
        [FromQuery] ExportAdminWalletLedgerRequest request,
        CancellationToken ct = default)
    {
        var query = new ExportWalletLedgerQuery(
            userId, request.FromDate, request.ToDate, request.TransactionType,
            request.MinAmount, request.MaxAmount, request.SearchTerm, request.Format, request.MaxRows ?? 10_000);
        return await Send(query, ct);
    }

    [HttpPost("{userId:guid}/credit")]
    public async Task<IActionResult> Credit(
        Guid userId,
        [FromBody] AdminWalletAdjustmentRequest request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken ct)
    {
        var effectiveKey = ResolveIdempotencyKey(idempotencyKey, "credit", userId);

        var command = new CreditWalletCommand(
            userId, request.Amount,
            WalletTransactionType.Credit, WalletReferenceType.Admin,
            HttpContext.TraceIdentifier, effectiveKey, HttpContext.TraceIdentifier,
            BuildAuditDescription("CREDIT", request.Reason, request.Description));

        return await Send(command, ct);
    }

    [HttpPost("{userId:guid}/debit-requests")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RequestDebit(
        Guid userId,
        [FromBody] AdminWalletDebitRequestPayload request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken ct)
    {
        var effectiveKey = ResolveIdempotencyKey(idempotencyKey, "debit-request", userId);

        var command = new RequestWalletDebitCommand(
            userId,
            request.Amount,
            request.Reason,
            request.Description,
            effectiveKey,
            request.ExpiryHours ?? 72);

        return await Send(command, ct);
    }

    [HttpGet("{userId:guid}/debit-requests/pending")]
    public async Task<IActionResult> GetPendingDebitRequests(Guid userId, CancellationToken ct)
        => await Send(new GetPendingDebitRequestsByUserQuery(userId), ct);

    [HttpPost("{userId:guid}/freeze")]
    public async Task<IActionResult> Freeze(
        Guid userId, [FromBody] FreezeWalletRequest request, CancellationToken ct)
        => await Send(new FreezeWalletCommand(userId, request.Reason), ct);

    [HttpPost("{userId:guid}/unfreeze")]
    public async Task<IActionResult> Unfreeze(Guid userId, CancellationToken ct)
        => await Send(new UnfreezeWalletCommand(userId), ct);

    [HttpGet("fraud/alerts")]
    public async Task<IActionResult> GetFraudAlerts([FromQuery] GetFraudAlertsRequest request, CancellationToken ct)
    {
        FraudAlertStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<FraudAlertStatus>(request.Status, true, out var parsedStatus))
            status = parsedStatus;

        FraudAlertSeverity? severity = null;
        if (!string.IsNullOrWhiteSpace(request.Severity)
            && Enum.TryParse<FraudAlertSeverity>(request.Severity, true, out var parsedSeverity))
            severity = parsedSeverity;

        var query = new GetFraudAlertsQuery(
            status, severity, request.UserId, request.Page, request.PageSize,
            request.FromDate, request.ToDate);
        return await Send(query, ct);
    }

    [HttpGet("fraud/alerts/count-open")]
    public async Task<IActionResult> GetOpenFraudAlertsCount(CancellationToken ct)
        => await Send(new GetOpenFraudAlertsCountQuery(), ct);

    [HttpGet("fraud/alerts/{id:guid}")]
    public async Task<IActionResult> GetFraudAlertById(Guid id, CancellationToken ct)
        => await Send(new GetFraudAlertByIdQuery(id), ct);

    [HttpPost("fraud/alerts/{id:guid}/mark-reviewed")]
    public async Task<IActionResult> MarkFraudAlertReviewed(
        Guid id, [FromBody] FraudAlertReviewRequest request, CancellationToken ct)
        => await Send(new MarkFraudAlertReviewedCommand(id, request.Note), ct);

    [HttpPost("fraud/alerts/{id:guid}/dismiss")]
    public async Task<IActionResult> DismissFraudAlert(
        Guid id, [FromBody] FraudAlertDismissRequest request, CancellationToken ct)
        => await Send(new DismissFraudAlertCommand(id, request.Note), ct);

    private string ResolveIdempotencyKey(string? headerValue, string operation, Guid userId)
    {
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            var trimmed = headerValue.Trim();
            if (trimmed.Length <= IdempotencyKeyMaxLength)
                return trimmed;
        }
        return $"admin-{operation}-{userId:N}-{HttpContext.TraceIdentifier}";
    }

    private static string BuildAuditDescription(string operation, string reason, string? extraNote)
    {
        var sb = new StringBuilder();
        sb.Append($"[ADMIN-{operation}] | Reason={reason}");
        if (!string.IsNullOrWhiteSpace(extraNote))
            sb.Append($" | Note={extraNote}");
        return sb.ToString();
    }
}
