using Application.Common.Interfaces.Admin.Inventory;
using Application.Common.Interfaces.User;
using Application.DTOs.Product;

namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/inventory")]
[Authorize(Roles = "Admin")]
public class AdminInventoryController : ControllerBase
{
    private readonly IAdminInventoryService _adminInventoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AdminInventoryController> _logger;

    public AdminInventoryController(
        IAdminInventoryService adminInventoryService,
        ICurrentUserService currentUserService,
        ILogger<AdminInventoryController> logger)
    {
        _adminInventoryService = adminInventoryService;
        _currentUserService = currentUserService;
        _logger = logger;
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
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _adminInventoryService.GetTransactionsAsync(variantId, transactionType, fromDate, toDate, page, pageSize);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockItems([FromQuery] int threshold = 5)
    {
        var result = await _adminInventoryService.GetLowStockItemsAsync(threshold);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("out-of-stock")]
    public async Task<IActionResult> GetOutOfStockItems()
    {
        var result = await _adminInventoryService.GetOutOfStockItemsAsync();
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpPost("adjust")]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized();

        var result = await _adminInventoryService.AdjustStockAsync(dto, userId.Value);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Stock adjusted successfully" });
    }

    [HttpPost("reconcile/{variantId}")]
    public async Task<IActionResult> ReconcileStock(int variantId)
    {
        var result = await _adminInventoryService.ReconcileStockAsync(variantId);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Stock reconciled successfully" });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var result = await _adminInventoryService.GetStatisticsAsync();
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }
}