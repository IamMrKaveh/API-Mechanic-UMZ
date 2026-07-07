using Application.Wallet.Features.Commands.CancelWithdrawal;
using Application.Wallet.Features.Commands.CompleteWalletTopUp;
using Application.Wallet.Features.Commands.InitiateTopUp;
using Application.Wallet.Features.Commands.RequestWithdrawal;
using Application.Wallet.Features.Queries.GetMyWithdrawals;
using Application.Wallet.Features.Queries.GetWalletBalance;
using Application.Wallet.Features.Queries.GetWalletLedger;
using Application.Wallet.Features.Queries.GetWithdrawalById;
using Application.Wallet.Features.Shared;
using Presentation.Wallet.Requests;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/wallet")]
[Authorize]
public class WalletController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
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
            RequestContext.UserId!.Value,
            request.Amount,
            request.Gateway);
        return await Send(cmd, ct);
    }

    [HttpGet("topup/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> TopUpCallback(
        [FromQuery] string Authority,
        [FromQuery] string Status,
        CancellationToken ct)
    {
        var cmd = new CompleteWalletTopUpCommand(Authority, Status);
        var result = await Mediator.Send(cmd, ct);
        var status = result.IsSuccess && result.Value is not null
            ? result.Value.StatusText
            : "unknown";
        return Redirect($"/dashboard/wallet/topup/callback?status={status}");
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
}