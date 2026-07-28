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
[EnableRateLimiting("admin-wallet")]
public sealed class AdminWalletWithdrawalController(IMediator mediator)
    : BaseApiController(mediator)
{
    [HttpGet("pending")]
    [SwaggerOperation(OperationId = "AdminWallet_PendingWithdrawals")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<WalletWithdrawalRequestDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingWithdrawals(
        [FromQuery] GetPendingWithdrawalsListRequest request,
        CancellationToken ct)
    {
        var query = new GetPendingWithdrawalsQuery(
            request.Status,
            request.Page,
            request.PageSize,
            request.FromDate,
            request.ToDate);

        return await Send(query, ct);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(OperationId = "AdminWallet_GetWithdrawal")]
    [ProducesResponseType(typeof(ApiResponse<WalletWithdrawalRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWithdrawalById(
        Guid id,
        CancellationToken ct)
    {
        var query = new GetWithdrawalByIdQuery(id);
        return await Send(query, ct);
    }

    [HttpPost("{id:guid}/approve")]
    [SwaggerOperation(OperationId = "AdminWallet_ApproveWithdrawal")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApproveWithdrawal(
        Guid id,
        CancellationToken ct)
    {
        var command = new ApproveWithdrawalCommand(id);
        return await Send(command, ct);
    }

    [HttpPost("{id:guid}/reject")]
    [SwaggerOperation(OperationId = "AdminWallet_RejectWithdrawal")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectWithdrawal(
        Guid id,
        [FromBody] RejectWithdrawalRequest request,
        CancellationToken ct)
    {
        var command = new RejectWithdrawalCommand(id, request.Reason);
        return await Send(command, ct);
    }

    [HttpPost("{id:guid}/mark-paid")]
    [SwaggerOperation(OperationId = "AdminWallet_MarkWithdrawalPaid")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkWithdrawalPaid(
        Guid id,
        [FromBody] MarkWithdrawalPaidRequest request,
        CancellationToken ct)
    {
        var command = new MarkWithdrawalPaidCommand(
            id,
            request.BankReferenceNumber);

        return await Send(command, ct);
    }
}
