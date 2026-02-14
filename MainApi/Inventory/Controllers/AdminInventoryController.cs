using Application.Inventory.Features.Queries.GetStatistics;
using Application.Inventory.Features.Queries.GetTransactions;

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
        var query = new GetLowStockProductsQuery(threshold);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("out-of-stock")]
    public async Task<IActionResult> GetOutOfStockItems()
    {
        // نیاز به Query جداگانه یا استفاده از فیلتر در Query اصلی دارد.
        // با فرض وجود GetOutOfStockProductsQuery:
        // var query = new GetOutOfStockProductsQuery(); 
        // var result = await _mediator.Send(query);
        // return ToActionResult(result);

        // موقتا:
        return StatusCode(501, "Implement GetOutOfStockProductsQuery");
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockCommand command)
    {
        // UserId را از توکن می‌گیریم، اگر در بدنه نیامده باشد
        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
        {
            command = command with { UserId = CurrentUser.UserId.Value };
        }

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkAdjustStockCommand command)
    {
        if (command.UserId <= 0 && CurrentUser.UserId.HasValue)
        {
            command = command with { UserId = CurrentUser.UserId.Value };
        }

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
        {
            command = command with { UserId = CurrentUser.UserId.Value };
        }

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var query = new GetInventoryStatisticsQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("status/{variantId}")]
    public async Task<IActionResult> GetInventoryStatus(int variantId)
    {
        var query = new GetInventoryStatusQuery(variantId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}