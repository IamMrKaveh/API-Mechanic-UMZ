using Application.Wallet.Features.Commands.CreditWallet;
using Application.Wallet.Features.Commands.DebitWallet;
using Application.Wallet.Features.Commands.DismissFraudAlert;
using Application.Wallet.Features.Commands.FreezeWallet;
using Application.Wallet.Features.Commands.MarkFraudAlertReviewed;
using Application.Wallet.Features.Commands.UnfreezeWallet;
using Application.Wallet.Features.Queries.ExportWalletLedger;
using Application.Wallet.Features.Queries.GetFraudAlertById;
using Application.Wallet.Features.Queries.GetFraudAlerts;
using Application.Wallet.Features.Queries.GetOpenFraudAlertsCount;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Queries.GetWalletsOverview;
using Application.Wallet.Features.Queries.GetWalletStatistics;
using Application.Wallet.Features.Shared;
using Domain.Wallet.Enums;
using Presentation.Wallet.Requests;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/wallets")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin-wallet")]
public sealed class AdminWalletController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletOverviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] GetWalletsOverviewRequest request,
        CancellationToken ct)
    {
        var query = new GetWalletsOverviewQuery(
            request.Search,
            request.IsFrozen,
            request.MinBalance,
            request.MaxBalance,
            request.CreatedFrom,
            request.CreatedTo,
            request.SortBy,
            request.Page,
            request.PageSize);

        return await Send(query, ct);
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<WalletStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(CancellationToken ct)
    {
        return await Send(new GetWalletStatisticsQuery(), ct);
    }

    [HttpGet("{userId:guid}/balance")]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
    {
        return await Send(new GetWalletBalanceQuery(userId), ct);
    }

    [HttpGet("{userId:guid}/ledger")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletLedgerEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedger(
        Guid userId,
        [FromQuery] GetAdminWalletLedgerRequest request,
        CancellationToken ct = default)
    {
        var query = new GetWalletLedgerQuery(
            userId,
            request.Page,
            request.PageSize,
            request.FromDate,
            request.ToDate,
            request.TransactionType,
            request.MinAmount,
            request.MaxAmount,
            request.SearchTerm);

        return await Send(query, ct);
    }

    [HttpGet("{userId:guid}/ledger/export")]
    [ProducesResponseType(typeof(ApiResponse<ExportWalletLedgerResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportLedger(
        Guid userId,
        [FromQuery] ExportAdminWalletLedgerRequest request,
        CancellationToken ct = default)
    {
        var query = new ExportWalletLedgerQuery(
            userId,
            request.FromDate,
            request.ToDate,
            request.TransactionType,
            request.MinAmount,
            request.MaxAmount,
            request.SearchTerm,
            request.Format,
            request.MaxRows ?? 10_000);

        return await Send(query, ct);
    }

    [HttpPost("{userId:guid}/credit")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
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
            BuildAuditDescription("CREDIT", request.Reason, request.Description));

        return await Send(command, ct);
    }

    [HttpPost("{userId:guid}/debit")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
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
            $"admin-debit-{userId}-{HttpContext.TraceIdentifier}",
            HttpContext.TraceIdentifier,
            BuildAuditDescription("DEBIT", request.Reason, request.Description));

        return await Send(command, ct);
    }

    [HttpPost("{userId:guid}/freeze")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Freeze(
        Guid userId,
        [FromBody] FreezeWalletRequest request,
        CancellationToken ct)
    {
        var command = new FreezeWalletCommand(userId, request.Reason);
        return await Send(command, ct);
    }

    [HttpPost("{userId:guid}/unfreeze")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unfreeze(
        Guid userId,
        CancellationToken ct)
    {
        var command = new UnfreezeWalletCommand(userId);
        return await Send(command, ct);
    }

    [HttpGet("fraud/alerts")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletFraudAlertDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFraudAlerts(
        [FromQuery] GetFraudAlertsRequest request,
        CancellationToken ct)
    {
        FraudAlertStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<FraudAlertStatus>(request.Status, ignoreCase: true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        FraudAlertSeverity? severity = null;
        if (!string.IsNullOrWhiteSpace(request.Severity)
            && Enum.TryParse<FraudAlertSeverity>(request.Severity, ignoreCase: true, out var parsedSeverity))
        {
            severity = parsedSeverity;
        }

        var query = new GetFraudAlertsQuery(
            status,
            severity,
            request.UserId,
            request.Page,
            request.PageSize,
            request.FromDate,
            request.ToDate);

        return await Send(query, ct);
    }

    [HttpGet("fraud/alerts/count-open")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOpenFraudAlertsCount(CancellationToken ct)
    {
        return await Send(new GetOpenFraudAlertsCountQuery(), ct);
    }

    [HttpGet("fraud/alerts/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WalletFraudAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFraudAlertById(Guid id, CancellationToken ct)
    {
        return await Send(new GetFraudAlertByIdQuery(id), ct);
    }

    [HttpPost("fraud/alerts/{id:guid}/mark-reviewed")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkFraudAlertReviewed(
        Guid id,
        [FromBody] FraudAlertReviewRequest request,
        CancellationToken ct)
    {
        var command = new MarkFraudAlertReviewedCommand(id, request.Note);
        return await Send(command, ct);
    }

    [HttpPost("fraud/alerts/{id:guid}/dismiss")]
    [ProducesResponseType(typeof(ApiResponse<Unit>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DismissFraudAlert(
        Guid id,
        [FromBody] FraudAlertDismissRequest request,
        CancellationToken ct)
    {
        var command = new DismissFraudAlertCommand(id, request.Note);
        return await Send(command, ct);
    }

    private static string BuildAuditDescription(
        string operation,
        string reason,
        string? extraNote)
    {
        var sb = new StringBuilder();
        sb.Append($"[ADMIN-{operation}] | Reason={reason}");
        if (!string.IsNullOrWhiteSpace(extraNote))
            sb.Append($" | Note={extraNote}");
        return sb.ToString();
    }
}