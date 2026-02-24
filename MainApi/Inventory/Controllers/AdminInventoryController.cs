namespace MainApi.Inventory.Controllers;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminInventoryController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

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
        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { UserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkAdjustStockCommand command)
    {
        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { UserId = CurrentUser.UserId.Value };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reconcile/{variantId}")]
    public async Task<IActionResult> ReconcileStock(int variantId)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var command = new ReconcileStockCommand(variantId, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("damage")]
    public async Task<IActionResult> RecordDamage([FromBody] RecordDamageCommand command)
    {
        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
            command = command with { UserId = CurrentUser.UserId.Value };

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

    /// <summary>
    /// Import دسته‌ای موجودی از تأمین‌کنندگان (Bulk StockIn)
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> BulkStockIn(
        [FromBody] BulkStockInRequest request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new BulkStockInCommand(
            request.Items.Select(i => new BulkStockInItemDto
            {
                VariantId = i.VariantId,
                Quantity = i.Quantity,
                Notes = i.Notes
            }).ToList(),
            CurrentUser.UserId.Value,
            request.SupplierReference);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    /// <summary>
    /// تأیید مرجوعی توسط ادمین و بازگشت موجودی به انبار
    /// </summary>
    [HttpPost("approve-return/{orderId}")]
    public async Task<IActionResult> ApproveReturn(
        int orderId,
        [FromBody] ApproveReturnRequest? request = null)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new ApproveReturnCommand
        {
            OrderId = orderId,
            AdminUserId = CurrentUser.UserId.Value,
            Reason = request?.Reason ?? "تأیید مرجوعی توسط ادمین"
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    /// <summary>
    /// Commit دستی موجودی برای سفارش (در صورت عدم Commit خودکار)
    /// </summary>
    [HttpPost("commit-order/{orderId}")]
    public async Task<IActionResult> CommitOrderInventory(int orderId)
    {
        var command = new CommitStockForOrderCommand(orderId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}