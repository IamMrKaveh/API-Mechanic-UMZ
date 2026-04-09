using Application.Inventory.Features.Commands.AdjustStock;
using Application.Inventory.Features.Commands.BulkAdjustStock;
using Application.Inventory.Features.Commands.BulkStockIn;
using Application.Inventory.Features.Commands.ReconcileStock;
using Application.Inventory.Features.Commands.RecordDamage;
using Application.Inventory.Features.Commands.ReverseInventoryTransaction;
using Application.Inventory.Features.Queries.GetInventoryStatistics;
using Application.Inventory.Features.Queries.GetInventoryStatus;
using Application.Inventory.Features.Queries.GetInventoryTransactions;
using Application.Inventory.Features.Queries.GetLowStockProducts;
using Application.Inventory.Features.Queries.GetOutOfStockProducts;
using Application.Inventory.Features.Queries.GetStockLedgerByVariant;
using Application.Inventory.Features.Queries.GetWarehouseStock;
using Application.Order.Features.Commands.ApproveReturn;
using Presentation.Inventory.Requests;

namespace Presentation.Inventory.Endpoints;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("transactions")]
    public async Task<IActionResult> GetInventoryTransactions(
        [FromQuery] Guid? variantId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetInventoryTransactionsQuery(variantId, transactionType, fromDate, toDate, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetStockLedger(
        [FromQuery] Guid variantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetStockLedgerByVariantQuery(variantId, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("warehouse-stock/{variantId}")]
    public async Task<IActionResult> GetWarehouseStock(Guid variantId)
    {
        var result = await _mediator.Send(new GetWarehouseStockQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPost("reverse")]
    public async Task<IActionResult> ReverseTransaction(
        [FromBody] ReverseInventoryTransactionRequest request)
    {
        var command = new ReverseInventoryCommand(
            request.VariantId,
            request.IdempotencyKey,
            request.Reason,
            CurrentUser.UserId);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int threshold = 5)
    {
        var result = await _mediator.Send(new GetLowStockProductsQuery(threshold));
        return ToActionResult(result);
    }

    [HttpGet("out-of-stock")]
    public async Task<IActionResult> GetOutOfStockItems()
    {
        var result = await _mediator.Send(new GetOutOfStockProductsQuery());
        return ToActionResult(result);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockRequest request)
    {
        var command = new AdjustStockCommand(
            request.VariantId,
            request.QuantityChange,
            CurrentUser.UserId,
            request.Reason);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkAdjustStockRequest request)
    {
        var command = new BulkAdjustStockCommand(
            request.VariantId,
            request.QuantityChange,
            CurrentUser.UserId,
            request.Reason);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reconcile/{variantId}")]
    public async Task<IActionResult> ReconcileStock(Guid variantId)
    {
        var command = new ReconcileStockCommand(variantId, 0, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("damage")]
    public async Task<IActionResult> RecordDamage([FromBody] RecordDamageRequest request)
    {
        var command = new RecordDamageCommand(
            request.VariantId,
            request.Quantity,
            CurrentUser.UserId,
            request.Reason);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _mediator.Send(new GetInventoryStatisticsQuery());
        return ToActionResult(result);
    }

    [HttpGet("status/{variantId}")]
    public async Task<IActionResult> GetInventoryStatus(Guid variantId)
    {
        var result = await _mediator.Send(new GetInventoryStatusQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> BulkStockIn([FromBody] BulkStockInRequest request)
    {
        var command = new BulkStockInCommand(
            request.VariantIds,
            request.Quantities,
            request.ReferenceNumbers,
            CurrentUser.UserId,
            request.Reason);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("approve-return/{orderId}")]
    public async Task<IActionResult> ApproveReturn(
        Guid orderId,
        [FromBody] ApproveReturnRequest? request = null)
    {
        var command = new ApproveReturnCommand(
            orderId,
            CurrentUser.UserId,
            request?.Reason ?? "تأیید مرجوعی توسط ادمین");

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}