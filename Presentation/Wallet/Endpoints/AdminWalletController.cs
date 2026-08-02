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
    private const string IdempotencyHeaderName = "Idempotency-Key";
    private const int IdempotencyKeyMaxLength = 128;

    [HttpGet("overview")]
    [SwaggerOperation(OperationId = "AdminWallet_GetOverview")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletOverviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] GetWalletsOverviewRequest request,
        CancellationToken ct)
    {
        var query = new GetWalletsOverviewQuery(
            request.Search, request.IsFrozen, request.MinBalance, request.MaxBalance,
            request.CreatedFrom, request.CreatedTo, request.SortBy, request.Page, request.PageSize);
        return await Send(query, ct);
    }

    [HttpGet("statistics")]
    [SwaggerOperation(OperationId = "AdminWallet_GetStatistics")]
    [ProducesResponseType(typeof(ApiResponse<WalletStatisticsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(CancellationToken ct)
        => await Send(new GetWalletStatisticsQuery(), ct);

    [HttpGet("{userId:guid}/balance")]
    [SwaggerOperation(OperationId = "AdminWallet_GetBalance")]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBalance(Guid userId, CancellationToken ct)
        => await Send(new GetWalletBalanceQuery(userId), ct);

    [HttpGet("{userId:guid}/ledger")]
    [SwaggerOperation(OperationId = "AdminWallet_GetLedger")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletLedgerEntryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
    [SwaggerOperation(OperationId = "AdminWallet_ExportLedger")]
    [ProducesResponseType(typeof(ApiResponse<ExportWalletLedgerResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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
    [SwaggerOperation(OperationId = "AdminWallet_Credit")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Credit(
        Guid userId,
        [FromBody] AdminWalletAdjustmentRequest request,
        [FromHeader(Name = IdempotencyHeaderName)] string? idempotencyKey,
        CancellationToken ct)
    {
        var effectiveKey = ResolveIdempotencyKey(idempotencyKey, "credit", userId);

        var command = new CreditWalletCommand(
            UserId: userId,
            Amount: request.Amount,
            TransactionType: WalletTransactionType.Credit,
            ReferenceType: WalletReferenceType.Admin,
            ReferenceId: HttpContext.TraceIdentifier,
            IdempotencyKey: effectiveKey,
            CorrelationId: HttpContext.TraceIdentifier,
            Description: BuildAuditDescription("CREDIT", request.Reason, request.Description, request.TransactionType),
            AdjustmentType: request.TransactionType);

        return await Send(command, ct);
    }

    [HttpPost("{userId:guid}/debit-requests")]
    [SwaggerOperation(OperationId = "AdminWallet_RequestDebit")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
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
    [SwaggerOperation(OperationId = "AdminWallet_GetPendingDebitRequests")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<WalletDebitRequestDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingDebitRequests(Guid userId, CancellationToken ct)
        => await Send(new GetPendingDebitRequestsByUserQuery(userId), ct);

    [HttpPost("{userId:guid}/freeze")]
    [SwaggerOperation(OperationId = "AdminWallet_Freeze")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Freeze(
        Guid userId, [FromBody] FreezeWalletRequest request, CancellationToken ct)
        => await Send(new FreezeWalletCommand(userId, request.Reason), ct);

    [HttpPost("{userId:guid}/unfreeze")]
    [SwaggerOperation(OperationId = "AdminWallet_Unfreeze")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Unfreeze(Guid userId, CancellationToken ct)
        => await Send(new UnfreezeWalletCommand(userId), ct);

    [HttpGet("fraud/alerts")]
    [SwaggerOperation(OperationId = "AdminWallet_GetFraudAlerts")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletFraudAlertDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFraudAlerts(
        [FromQuery] GetFraudAlertsRequest request, CancellationToken ct)
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
    [SwaggerOperation(OperationId = "AdminWallet_GetOpenFraudAlertsCount")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOpenFraudAlertsCount(CancellationToken ct)
        => await Send(new GetOpenFraudAlertsCountQuery(), ct);

    [HttpGet("fraud/alerts/{id:guid}")]
    [SwaggerOperation(OperationId = "AdminWallet_GetFraudAlertById")]
    [ProducesResponseType(typeof(ApiResponse<WalletFraudAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFraudAlertById(Guid id, CancellationToken ct)
        => await Send(new GetFraudAlertByIdQuery(id), ct);

    [HttpPost("fraud/alerts/{id:guid}/mark-reviewed")]
    [SwaggerOperation(OperationId = "AdminWallet_MarkFraudAlertReviewed")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkFraudAlertReviewed(
        Guid id, [FromBody] FraudAlertReviewRequest request, CancellationToken ct)
        => await Send(new MarkFraudAlertReviewedCommand(id, request.Note), ct);

    [HttpPost("fraud/alerts/{id:guid}/dismiss")]
    [SwaggerOperation(OperationId = "AdminWallet_DismissFraudAlert")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
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

    private static string BuildAuditDescription(
        string operation,
        string reason,
        string? extraNote,
        AdminWalletAdjustmentType adjustmentType)
    {
        var sb = new StringBuilder();
        sb.Append($"[ADMIN-{operation}|{adjustmentType}] | Reason={reason}");
        if (!string.IsNullOrWhiteSpace(extraNote))
            sb.Append($" | Note={extraNote}");
        return sb.ToString();
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
