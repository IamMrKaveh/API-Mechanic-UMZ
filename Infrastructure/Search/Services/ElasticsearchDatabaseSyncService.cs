namespace Infrastructure.Search.Services;

public class ElasticsearchDatabaseSyncService : ISearchDatabaseSyncService
{
    private readonly LedkaContext _context;
    private readonly ISearchService _searchService;
    private readonly IElasticBulkService _bulkService;
    private readonly ILogger<ElasticsearchDatabaseSyncService> _logger;

    public ElasticsearchDatabaseSyncService(
        LedkaContext context,
        ISearchService searchService,
        IElasticBulkService bulkService,
        ILogger<ElasticsearchDatabaseSyncService> logger)
    {
        _context = context;
        _searchService = searchService;
        _bulkService = bulkService;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct = default)
    {
        await FullSyncAsync(ct);
        _logger.LogInformation("Sync Completed");
    }

    public async Task SyncProductAsync(int productId, CancellationToken ct = default)
    {
        var product = await _context.Products
            .Include(p => p.Brand)
                .ThenInclude(cg => cg.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for sync", productId);
            return;
        }

        var document = MapToSearchDocument(product);
        await _searchService.IndexProductAsync(document, ct);

        _logger.LogInformation("Product {ProductId} synced to Elasticsearch", productId);
    }

    public async Task SyncAllProductsAsync(CancellationToken ct = default)
    {
        var batchSize = 500;
        var page = 0;
        var hasMore = true;

        while (hasMore && !ct.IsCancellationRequested)
        {
            var products = await _context.Products
                .Include(p => p.Brand)
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

            var documents = products.Select(MapToSearchDocument).ToList();
            await _bulkService.BulkIndexProductsAsync(documents, ct);

            _logger.LogInformation("Synced batch {Page} with {Count} products", page + 1, documents.Count);
            page++;

            await Task.Delay(100, ct);
        }

        _logger.LogInformation("Completed syncing {Pages} batches of products", page);
    }

    public async Task SyncCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        var category = await _context.Categories
            .Include(c => c.Images)
            .Include(c => c.Brands)
                .ThenInclude(cg => cg.Products)
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category == null)
        {
            _logger.LogWarning("Category {CategoryId} not found for sync", categoryId);
            return;
        }

        var document = new CategorySearchDocument
        {
            CategoryId = category.Id,
            Name = category.Name,
            Slug = string.Empty,
            IsActive = category.IsActive,
            ProductCount = category.Brands
                .SelectMany(cg => cg.Products)
                .Count(p => p.IsActive && !p.IsDeleted),
            Icon = category.Images?.FirstOrDefault()?.FilePath
        };

        await _searchService.IndexCategoryAsync(document, ct);

        _logger.LogInformation("Category {CategoryId} synced to Elasticsearch", categoryId);
    }

    public async Task SyncAllCategoriesAsync(CancellationToken ct = default)
    {
        var categories = await _context.Categories
            .Include(c => c.Images)
            .Include(c => c.Brands)
                .ThenInclude(cg => cg.Products)
            .Where(c => c.IsActive && !c.IsDeleted)
            .ToListAsync(ct);

        var documents = categories.Select(c => new CategorySearchDocument
        {
            CategoryId = c.Id,
            Name = c.Name,
            Slug = string.Empty,
            IsActive = c.IsActive,
            ProductCount = c.Brands
                .SelectMany(cg => cg.Products)
                .Count(p => p.IsActive && !p.IsDeleted),
            Icon = c.Images?.FirstOrDefault()?.FilePath
        }).ToList();

        await _bulkService.BulkIndexCategoriesAsync(documents, ct);

        _logger.LogInformation("Synced {Count} categories to Elasticsearch", documents.Count);
    }

    public async Task SyncBrandAsync(int BrandId, CancellationToken ct = default)
    {
        var group = await _context.Brands
            .Include(cg => cg.Images)
            .Include(cg => cg.Category)
            .Include(cg => cg.Products)
            .FirstOrDefaultAsync(cg => cg.Id == BrandId, ct);

        if (group == null)
        {
            _logger.LogWarning("Brand {BrandId} not found for sync", BrandId);
            return;
        }

        var document = new BrandSearchDocument
        {
            BrandId = group.Id,
            Name = group.Name,
            Slug = string.Empty,
            CategoryId = group.CategoryId,
            CategoryName = group.Category.Name,
            IsActive = group.IsActive,
            ProductCount = group.Products.Count(p => p.IsActive && !p.IsDeleted),
            Icon = group.Images?.FirstOrDefault()?.FilePath
        };

        await _searchService.IndexBrandAsync(document, ct);

        _logger.LogInformation("Brand {BrandId} synced to Elasticsearch", BrandId);
    }

    public async Task SyncAllBrandsAsync(CancellationToken ct = default)
    {
        var groups = await _context.Brands
            .Include(cg => cg.Images)
            .Include(cg => cg.Category)
            .Include(cg => cg.Products)
            .Where(cg => cg.IsActive && !cg.IsDeleted)
            .ToListAsync(ct);

        var documents = groups.Select(cg => new BrandSearchDocument
        {
            BrandId = cg.Id,
            Name = cg.Name,
            Slug = string.Empty,
            CategoryId = cg.CategoryId,
            CategoryName = cg.Category.Name,
            IsActive = cg.IsActive,
            ProductCount = cg.Products.Count(p => p.IsActive && !p.IsDeleted),
            Icon = cg.Images?.FirstOrDefault()?.FilePath
        }).ToList();

        await _bulkService.BulkIndexBrandsAsync(documents, ct);

        _logger.LogInformation("Synced {Count} category groups to Elasticsearch", documents.Count);
    }

    public async Task FullSyncAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting full sync from Supabase database to Elasticsearch");

        await SyncAllCategoriesAsync(ct);
        await SyncAllBrandsAsync(ct);
        await SyncAllProductsAsync(ct);

        _logger.LogInformation("Full sync completed");
    }

    private ProductSearchDocument MapToSearchDocument(Domain.Product.Product product)
    {
        var brand = product.Variants
            .SelectMany(v => v.VariantAttributes)
            .Where(va => va.AttributeValue?.AttributeType?.Name.Equals("Brand", StringComparison.OrdinalIgnoreCase) == true)
            .Select(va => va.AttributeValue!.DisplayValue)
            .FirstOrDefault() ?? string.Empty;

        var tags = product.Variants
            .SelectMany(v => v.VariantAttributes)
            .Select(va => va.AttributeValue?.DisplayValue)
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .ToList();

        return new ProductSearchDocument
        {
            ProductId = product.Id,
            Name = product.Name.Value,
            Description = product.Description ?? string.Empty,
            Slug = product.Name.Value.Replace(" ", "-"),
            CategoryName = product.Brand?.Category?.Name.Value ?? string.Empty,
            CategoryId = product.Brand?.Category?.Id ?? 0,
            BrandName = product.Brand?.Name.Value ?? string.Empty,
            BrandId = product.BrandId,
            Brand = product.Brand!,
            Price = product.Stats.MinPrice.Amount,
            DiscountedPrice = product.Variants.Any(v => v.SellingPrice.Amount < v.OriginalPrice.Amount)
                ? product.Variants.Min(v => Convert.ToDouble(v.SellingPrice.Amount))
                : null,
            DiscountPercentage = product.Variants.Any(v => v.SellingPrice.Amount < v.OriginalPrice.Amount && v.OriginalPrice.Amount > 0)
                ? Convert.ToDouble(
                    ((product.Variants.First(v => v.SellingPrice.Amount < v.OriginalPrice.Amount).OriginalPrice.Amount -
                          product.Variants.First(v => v.SellingPrice.Amount < v.OriginalPrice.Amount).SellingPrice.Amount) /
                          product.Variants.First(v => v.SellingPrice.Amount < v.OriginalPrice.Amount).OriginalPrice.Amount * 100)
                    )
                : null,
            Images = product.Images?.Select(i => i.FilePath).ToList() ?? new List<string>(),
            ImageUrl = product.Images?.FirstOrDefault()?.FilePath ?? string.Empty,
            Icon = product.Images?.FirstOrDefault()?.FilePath,
            IsActive = product.IsActive,
            InStock = product.Stats.TotalStock > 0 || product.Variants.Any(v => v.IsUnlimited),
            StockQuantity = product.Stats.TotalStock,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt ?? product.CreatedAt,
            Tags = tags!,
        };
    }
}