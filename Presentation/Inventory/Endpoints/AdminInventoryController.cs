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
using Application.Inventory.Features.Shared;
using Application.Order.Features.Commands.ApproveReturn;
using MapsterMapper;

namespace Presentation.Inventory.Endpoints;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController(IMediator mediator, IMapper mapper) : BaseApiController(mediator)
{
    [HttpGet("transactions")]
    public async Task<IActionResult> GetInventoryTransactions(
        [FromQuery] Guid? variantId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await Mediator.Send(
            new GetInventoryTransactionsQuery(variantId, transactionType, fromDate, toDate, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> GetStockLedger(
        [FromQuery] Guid variantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetStockLedgerByVariantQuery(variantId, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("warehouse-stock/{variantId}")]
    public async Task<IActionResult> GetWarehouseStock(Guid variantId)
    {
        var result = await Mediator.Send(new GetWarehouseStockQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPost("reverse")]
    public async Task<IActionResult> ReverseTransaction([FromBody] ReverseInventoryDto dto)
    {
        var command = new ReverseInventoryCommand(
            dto.VariantId,
            dto.IdempotencyKey,
            dto.Reason,
            CurrentUser.UserId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int threshold = 5)
    {
        var result = await Mediator.Send(new GetLowStockProductsQuery(threshold));
        return ToActionResult(result);
    }

    [HttpGet("out-of-stock")]
    public async Task<IActionResult> GetOutOfStockItems()
    {
        var result = await Mediator.Send(new GetOutOfStockProductsQuery());
        return ToActionResult(result);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustStockDto dto)
    {
        var command = new AdjustStockCommand(dto.VariantId, dto.QuantityChange, CurrentUser.UserId, dto.Reason);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("bulk-adjust")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] BulkAdjustStockDto dto)
    {
        var command = new BulkAdjustStockCommand(dto.Items, CurrentUser.UserId, dto.Reason);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reconcile/{variantId}")]
    public async Task<IActionResult> ReconcileStock(Guid variantId)
    {
        var command = new ReconcileStockCommand(variantId, 0, CurrentUser.UserId);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("damage")]
    public async Task<IActionResult> RecordDamage([FromBody] RecordDamageDto dto)
    {
        var command = new RecordDamageCommand(dto.VariantId, dto.Quantity, CurrentUser.UserId, dto.Reason);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await Mediator.Send(new GetInventoryStatisticsQuery());
        return ToActionResult(result);
    }

    [HttpGet("status/{variantId}")]
    public async Task<IActionResult> GetInventoryStatus(Guid variantId)
    {
        var result = await Mediator.Send(new GetInventoryStatusQuery(variantId));
        return ToActionResult(result);
    }

    [HttpPost("import")]
    public async Task<IActionResult> BulkStockIn([FromBody] BulkStockInDto dto)
    {
        var command = new BulkStockInCommand(dto.Items, CurrentUser.UserId, dto.Reason);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("approve-return/{orderId}")]
    public async Task<IActionResult> ApproveReturn(Guid orderId, [FromBody] ApproveReturnDto? dto = null)
    {
        var command = new ApproveReturnCommand(
            orderId,
            CurrentUser.UserId,
            dto?.Reason ?? "تأیید مرجوعی توسط ادمین");
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}