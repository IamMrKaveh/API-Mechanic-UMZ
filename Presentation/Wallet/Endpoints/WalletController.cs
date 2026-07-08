using Application.Wallet.Features.Commands.CancelWalletTransfer;
using Application.Wallet.Features.Commands.CancelWithdrawal;
using Application.Wallet.Features.Commands.CompleteWalletTopUp;
using Application.Wallet.Features.Commands.ConfirmWalletTransfer;
using Application.Wallet.Features.Commands.InitiateWalletTopUp;
using Application.Wallet.Features.Commands.InitiateWalletTransfer;
using Application.Wallet.Features.Commands.MarkWithdrawalPaid;
using Application.Wallet.Features.Commands.RejectWithdrawal;
using Application.Wallet.Features.Commands.RequestWithdrawal;
using Application.Wallet.Features.Queries.GetMyWithdrawals;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Queries.GetWithdrawalById;
using Application.Wallet.Features.Queries.PreviewWalletTransfer;
using Application.Wallet.Features.Shared;
using Infrastructure.Common.Options;
using Presentation.Wallet.Requests;
using System.Web;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/wallet")]
[Authorize]
public class WalletController(
    IMediator mediator,
    IMapper mapper,
    IOptions<FrontendUrlsOptions> frontendOptions)
    : BaseApiController(mediator, mapper)
{
    private readonly FrontendUrlsOptions _frontendOptions = frontendOptions.Value;

    [HttpGet("balance")]
    [ProducesResponseType(typeof(ApiResponse<WalletDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken ct)
    {
        var query = new GetWalletBalanceQuery(RequestContext.UserId!.Value);
        return await Send(query, ct);
    }

    [HttpGet("ledger")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletLedgerEntryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedger(
        [FromQuery] GetWalletLedgerRequest request,
        CancellationToken ct)
    {
        var query = new GetWalletLedgerQuery(
            RequestContext.UserId!.Value,
            request.Page,
            request.PageSize);

        return await Send(query, ct);
    }

    [HttpPost("topup/initiate")]
    [ProducesResponseType(typeof(ApiResponse<InitiateTopUpResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitiateTopUp(
            [FromBody] InitiateTopUpRequest request,
            CancellationToken ct)
    {
        var cmd = new InitiateWalletTopUpCommand(
            request.Amount,
            request.Gateway);
        return await Send(cmd, ct);
    }

    [HttpGet("topup/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> TopUpCallback(
            [FromQuery(Name = "Authority")] string? authority,
            [FromQuery(Name = "Status")] string? status,
            CancellationToken ct)
    {
        var frontendBase = ResolveFrontendBaseUrl().TrimEnd('/');
        var callbackPath = string.IsNullOrWhiteSpace(_frontendOptions.WalletTopUpCallbackPath)
            ? "/dashboard/wallet/topup/callback"
            : _frontendOptions.WalletTopUpCallbackPath;

        var effectiveAuthority = authority ?? string.Empty;
        var effectiveStatus = string.IsNullOrWhiteSpace(status) ? "NOK" : status;

        var cmd = new CompleteWalletTopUpCommand(effectiveAuthority, effectiveStatus);
        var result = await Mediator.Send(cmd, ct);

        string statusText;
        string? refId = null;
        decimal? amount = null;

        if (result.IsSuccess && result.Value is not null)
        {
            statusText = result.Value.StatusText;
            refId = result.Value.RefId;
            amount = result.Value.Amount;
        }
        else
        {
            statusText = "unknown";
        }

        var query = new List<string>
        {
            $"status={HttpUtility.UrlEncode(statusText)}",
            $"authority={HttpUtility.UrlEncode(effectiveAuthority)}"
        };
        if (!string.IsNullOrWhiteSpace(refId))
            query.Add($"refId={HttpUtility.UrlEncode(refId)}");
        if (amount is not null)
            query.Add($"amount={amount.Value}");

        var redirectUrl = $"{frontendBase}{callbackPath}?{string.Join('&', query)}";
        return Redirect(redirectUrl);
    }

    [HttpPost("topup/complete")]
    [ProducesResponseType(typeof(ApiResponse<CompleteWalletTopUpResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteTopUp(
        [FromBody] CompleteTopUpRequest request,
        CancellationToken ct)
    {
        var cmd = new CompleteWalletTopUpCommand(request.Authority ?? string.Empty, request.Status ?? "NOK");
        return await Send(cmd, ct);
    }

    [HttpPost("withdrawals")]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestWithdrawal(
        [FromBody] RequestWithdrawalRequest request,
        CancellationToken ct)
    {
        var cmd = new RequestWithdrawalCommand(
            RequestContext.UserId!.Value,
            request.Amount,
            request.Iban,
            request.AccountHolder,
            request.Description);
        return await Send(cmd, ct);
    }

    [HttpGet("withdrawals")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletWithdrawalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyWithdrawals(
        [FromQuery] GetWithdrawalsListRequest request,
        CancellationToken ct)
    {
        var query = new GetMyWithdrawalsQuery(
            RequestContext.UserId!.Value,
            request.Page,
            request.PageSize);
        return await Send(query, ct);
    }

    [HttpGet("withdrawals/{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WalletWithdrawalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithdrawalById(
        Guid id,
        CancellationToken ct)
    {
        var query = new GetWithdrawalByIdQuery(
            id,
            RequestContext.UserId,
            IsAdmin: false);
        return await Send(query, ct);
    }

    [HttpPost("withdrawals/{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelWithdrawal(
        Guid id,
        CancellationToken ct)
    {
        var cmd = new CancelWithdrawalCommand(id, RequestContext.UserId!.Value);
        return await Send(cmd, ct);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectWithdrawal(
        Guid id,
        [FromBody] RejectWithdrawalRequest request,
        CancellationToken ct)
    {
        var cmd = new RejectWithdrawalCommand(id, RequestContext.UserId!.Value, request.Reason);
        return await Send(cmd, ct);
    }

    [HttpPost("{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkWithdrawalPaid(
        Guid id,
        [FromBody] MarkWithdrawalPaidRequest request,
        CancellationToken ct)
    {
        var cmd = new MarkWithdrawalPaidCommand(
            id,
            RequestContext.UserId!.Value,
            request.BankReferenceNumber);
        return await Send(cmd, ct);
    }

    [HttpPost("transfer/preview")]
    [ProducesResponseType(typeof(ApiResponse<WalletTransferPreviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewTransfer(
        [FromBody] PreviewWalletTransferRequest request,
        CancellationToken ct)
    {
        var query = new PreviewWalletTransferQuery(
            RequestContext.UserId!.Value,
            request.RecipientPhoneNumber,
            request.Amount);
        return await Send(query, ct);
    }

    [HttpPost("transfer/initiate")]
    [ProducesResponseType(typeof(ApiResponse<InitiateWalletTransferResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InitiateTransfer(
        [FromBody] InitiateWalletTransferRequest request,
        CancellationToken ct)
    {
        var cmd = new InitiateWalletTransferCommand(
            RequestContext.UserId!.Value,
            request.RecipientPhoneNumber,
            request.Amount,
            request.Description);
        return await Send(cmd, ct);
    }

    [HttpPost("transfer/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmWalletTransferResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmTransfer(
        [FromBody] ConfirmWalletTransferRequest request,
        CancellationToken ct)
    {
        var cmd = new ConfirmWalletTransferCommand(
            request.TransferId,
            RequestContext.UserId!.Value,
            request.OtpCode);
        return await Send(cmd, ct);
    }

    [HttpPost("transfer/{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelTransfer(
        Guid id,
        CancellationToken ct)
    {
        var cmd = new CancelWalletTransferCommand(id, RequestContext.UserId!.Value);
        return await Send(cmd, ct);
    }

    private string ResolveFrontendBaseUrl()
    {
        var origin = Request.Headers["Origin"].ToString();
        if (!string.IsNullOrWhiteSpace(origin) && Uri.IsWellFormedUriString(origin, UriKind.Absolute))
            return origin;

        if (!string.IsNullOrWhiteSpace(_frontendOptions.BaseUrl))
            return _frontendOptions.BaseUrl;

        return _frontendOptions.LocalHostUrl;
    }
}