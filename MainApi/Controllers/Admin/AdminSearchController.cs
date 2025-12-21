namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/[controller]")]
public class AdminSearchController : ControllerBase
{
    private readonly ElasticsearchInitialSyncService _syncService;
    private readonly IElasticIndexManager _indexManager;
    private readonly ILogger<AdminSearchController> _logger;

    public AdminSearchController(
        ElasticsearchInitialSyncService syncService,
        IElasticIndexManager indexManager,
        ILogger<AdminSearchController> logger)
    {
        _syncService = syncService;
        _indexManager = indexManager;
        _logger = logger;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAllData(CancellationToken ct = default)
    {
        try
        {
            await _syncService.SyncAllDataAsync(ct);
            return Ok(new { message = "همگام‌سازی با موفقیت انجام شد" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Elasticsearch sync");
            return StatusCode(500, new { error = "خطا در همگام‌سازی" });
        }
    }

    [HttpPost("recreate-indices")]
    public async Task<IActionResult> RecreateIndices(CancellationToken ct = default)
    {
        try
        {
            var indices = new[] { "products_v1", "categories_v1", "categorygroups_v1" };
            foreach (var index in indices)
            {
                if (await _indexManager.IndexExistsAsync(index, ct))
                {
                    await _indexManager.DeleteIndexAsync(index, ct);
                }
            }

            var success = await _indexManager.CreateAllIndicesAsync(ct);

            if (success)
            {
                return Ok(new { message = "ایندکس‌ها با موفقیت بازسازی شدند" });
            }
            else
            {
                return StatusCode(500, new { error = "خطا در ایجاد ایندکس‌ها" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recreating indices");
            return StatusCode(500, new { error = "خطا در بازسازی ایندکس‌ها" });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetIndexStats(CancellationToken ct = default)
    {
        try
        {
            var indices = new[] { "products_v1", "categories_v1", "categorygroups_v1" };
            var stats = new List<object>();

            foreach (var index in indices)
            {
                var exists = await _indexManager.IndexExistsAsync(index, ct);
                stats.Add(new
                {
                    index,
                    exists
                });
            }

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting index stats");
            return StatusCode(500, new { error = "خطا در دریافت آمار" });
        }
    }
}