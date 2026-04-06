using Presentation.Base.Controllers.v1;

namespace Presentation.Inventory.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("transactions")]
    public async Task<IActionResult> GetInventoryTransactions(
        [FromQuery] int? variantId,
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
        [FromQuery] int variantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetStockLedgerByVariantQuery(variantId, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("warehouse-stock/{variantId}")]
    public async Task<IActionResult> GetWarehouseStock(int variantId)
    {
        var result = await _mediator.Send(new GetWarehouseStockQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPost("reverse")]
    public async Task<IActionResult> ReverseTransaction([FromBody] ReverseInventoryTransactionCommand command)
    {
        var commandWithAdmin = command with { AdminUserId = CurrentUser.UserId.Value };
        var result = await _mediator.Send(commandWithAdmin);
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
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkAdjustStockCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reconcile/{variantId}")]
    public async Task<IActionResult> ReconcileStock(int variantId)
    {
        var command = new ReconcileStockCommand(variantId, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("damage")]
    public async Task<IActionResult> RecordDamage([FromBody] RecordDamageCommand command)
    {
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
    public async Task<IActionResult> GetInventoryStatus(int variantId)
    {
        var query = new GetInventoryStatusQuery(variantId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> BulkStockIn([FromBody] BulkStockInRequest request)
    {
        var command = new BulkStockInCommand(
            request.Items.Select(i => new BulkStockInItemDto
            {
                VariantId = i.VariantId,
                Quantity = i.Quantity,
                Notes = i.Notes
            }).ToList(),
            CurrentUser.UserId,
            request.SupplierReference);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("approve-return/{orderId}")]
    public async Task<IActionResult> ApproveReturn(
        int orderId,
        [FromBody] ApproveReturnRequest? request = null)
    {
        var command = new ApproveReturnCommand
        {
            OrderId = orderId,
            AdminUserId = CurrentUser.UserId,
            Reason = request?.Reason ?? "تأیید مرجوعی توسط ادمین"
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("commit-order/{orderId}")]
    public async Task<IActionResult> CommitOrderInventory(int orderId)
    {
        var command = new CommitStockForOrderCommand(orderId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}