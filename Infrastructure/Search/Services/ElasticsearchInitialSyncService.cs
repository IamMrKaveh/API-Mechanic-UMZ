namespace Infrastructure.Search.Services;

public class ElasticsearchInitialSyncService
{
    private readonly LedkaContext _context;
    private readonly IElasticBulkService _bulkService;
    private readonly ILogger<ElasticsearchInitialSyncService> _logger;

    public ElasticsearchInitialSyncService(
        LedkaContext context,
        IElasticBulkService bulkService,
        ILogger<ElasticsearchInitialSyncService> logger)
    {
        _context = context;
        _bulkService = bulkService;
        _logger = logger;
    }

    public async Task SyncAllDataAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting initial sync from Supabase to Elasticsearch");

        await SyncCategoriesAsync(ct);
        await SyncCategoryGroupsAsync(ct);
        await SyncProductsAsync(ct);

        _logger.LogInformation("Initial sync completed successfully");
    }

    private async Task SyncCategoriesAsync(CancellationToken ct)
    {
        var categories = await _context.Categories
            .Include(c => c.CategoryGroups)
                .ThenInclude(cg => cg.Products)
            .Where(c => c.IsActive && !c.IsDeleted)
            .ToListAsync(ct);

        if (!categories.Any())
        {
            _logger.LogWarning("No categories found in database");
            return;
        }

        var documents = categories.Select(c => new CategorySearchDocument
        {
            CategoryId = c.Id,
            Name = c.Name,
            Slug = string.Empty,
            IsActive = c.IsActive,
            ProductCount = c.CategoryGroups
                .SelectMany(cg => cg.Products)
                .Count(p => p.IsActive && !p.IsDeleted)
        }).ToList();

        await _bulkService.BulkIndexCategoriesAsync(documents, ct);
        _logger.LogInformation("Synced {Count} categories from Supabase", documents.Count);
    }

    private async Task SyncCategoryGroupsAsync(CancellationToken ct)
    {
        var categoryGroups = await _context.CategoryGroups
            .Include(cg => cg.Category)
            .Include(cg => cg.Products)
            .Where(cg => cg.IsActive && !cg.IsDeleted)
            .ToListAsync(ct);

        if (!categoryGroups.Any())
        {
            _logger.LogWarning("No category groups found in database");
            return;
        }

        var documents = categoryGroups.Select(cg => new CategoryGroupSearchDocument
        {
            CategoryGroupId = cg.Id,
            Name = cg.Name,
            Slug = string.Empty,
            CategoryId = cg.CategoryId,
            CategoryName = cg.Category.Name,
            IsActive = cg.IsActive,
            ProductCount = cg.Products.Count(p => p.IsActive && !p.IsDeleted)
        }).ToList();

        await _bulkService.BulkIndexCategoryGroupsAsync(documents, ct);
        _logger.LogInformation("Synced {Count} category groups from Supabase", documents.Count);
    }

    private async Task SyncProductsAsync(CancellationToken ct)
    {
        var batchSize = 1000;
        var page = 0;
        var hasMore = true;
        var totalSynced = 0;

        while (hasMore && !ct.IsCancellationRequested)
        {
            var products = await _context.Products
                .Include(p => p.CategoryGroup)
                    .ThenInclude(cg => cg.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images)
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Id)
                .Skip(page * batchSize)
                .Take(batchSize)
                .ToListAsync(ct);

            if (!products.Any())
            {
                hasMore = false;
                continue;
            }

            var documents = products.Select(p => new ProductSearchDocument
            {
                ProductId = p.Id,
                Name = p.Name,
                Description = p.Description ?? string.Empty,
                Slug = string.Empty,
                Sku = p.Sku ?? string.Empty,
                CategoryName = p.CategoryGroup?.Category?.Name ?? string.Empty,
                CategoryId = p.CategoryGroup?.Category?.Id ?? 0,
                CategoryGroupName = p.CategoryGroup?.Name ?? string.Empty,
                CategoryGroupId = p.CategoryGroupId,
                Price = p.MinPrice,
                DiscountedPrice = p.Variants.Any(v => v.SellingPrice < v.OriginalPrice)
                    ? (float?)p.Variants.Min(v => v.SellingPrice)
                    : null,
                DiscountPercentage = p.Variants.Any(v => v.SellingPrice < v.OriginalPrice && v.OriginalPrice > 0)
                    ? (float?)((p.Variants.First(v => v.SellingPrice < v.OriginalPrice).OriginalPrice -
                               p.Variants.First(v => v.SellingPrice < v.OriginalPrice).SellingPrice) /
                              p.Variants.First(v => v.SellingPrice < v.OriginalPrice).OriginalPrice * 100)
                    : null,
                Images = p.Images?.Select(i => i.FilePath).ToList() ?? new List<string>(),
                ImageUrl = p.Images?.FirstOrDefault()?.FilePath ?? string.Empty,
                IsActive = p.IsActive,
                InStock = p.TotalStock > 0 || p.Variants.Any(v => v.IsUnlimited),
                StockQuantity = p.TotalStock,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt ?? p.CreatedAt,
                Tags = new List<string>(),
                Brand = string.Empty
            }).ToList();

            await _bulkService.BulkIndexProductsAsync(documents, ct);
            totalSynced += documents.Count;

            _logger.LogInformation("Synced products batch {Page} with {Count} items from Supabase", page + 1, documents.Count);
            page++;

            await Task.Delay(100, ct);
        }

        _logger.LogInformation("Total synced {Count} products from Supabase in {Pages} batches", totalSynced, page);
    }
}