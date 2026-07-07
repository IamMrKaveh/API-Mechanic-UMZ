using Application.Wallet.Features.Commands.ApproveWithdrawal;
using Application.Wallet.Features.Commands.MarkWithdrawalPaid;
using Application.Wallet.Features.Commands.RejectWithdrawal;
using Application.Wallet.Features.Queries.GetPendingWithdrawals;
using Application.Wallet.Features.Queries.GetWithdrawalById;
using Application.Wallet.Features.Shared;
using Presentation.Wallet.Requests;

namespace Presentation.Wallet.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/wallets/withdrawals")]
[Authorize(Roles = "Admin")]
public class AdminWalletWithdrawalController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet("pending")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletWithdrawalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingWithdrawals(
        [FromQuery] GetPendingWithdrawalsListRequest request,
        CancellationToken ct)
    {
        var query = new GetPendingWithdrawalsQuery(
            request.Status,
            request.Page,
            request.PageSize);
        return await Send(query, ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WalletWithdrawalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithdrawalById(
        Guid id,
        CancellationToken ct)
    {
        var query = new GetWithdrawalByIdQuery(id, RequestContext.UserId, IsAdmin: true);
        return await Send(query, ct);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveWithdrawal(
        Guid id,
        CancellationToken ct)
    {
        var cmd = new ApproveWithdrawalCommand(id, RequestContext.UserId!.Value);
        return await Send(cmd, ct);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
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
}