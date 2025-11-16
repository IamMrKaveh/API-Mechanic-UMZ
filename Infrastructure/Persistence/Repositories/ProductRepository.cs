using Application.DTOs;

namespace Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly LedkaContext _context;

    public ProductRepository(LedkaContext context)
    {
        _context = context;
    }

    public IQueryable<Domain.Product.Product> GetProductsQuery(ProductSearchDto search)
    {
        var query = _context.Set<Domain.Product.Product>()
            .Include(p => p.CategoryGroup.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Images)
            .AsQueryable();

        if (search.IncludeDeleted == true)
        {
            query = query.IgnoreQueryFilters().Where(p => p.IsDeleted);
        }
        else if (search.IncludeInactive == true)
        {
            query = query.IgnoreQueryFilters().Where(p => !p.IsActive);
        }
        else
        {
            query = query.Where(p => p.IsActive && !p.IsDeleted);
        }


        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var pattern = $"%{search.Name}%";
            query = query.Where(p => p.Name != null && EF.Functions.ILike(p.Name, pattern));
        }

        if (search.CategoryId.HasValue)
            query = query.Where(p => p.CategoryGroup.CategoryId == search.CategoryId);
        if (search.MinPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.SellingPrice >= search.MinPrice.Value));
        if (search.MaxPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.SellingPrice <= search.MaxPrice.Value));
        if (search.InStock == true)
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0));
        if (search.HasDiscount == true)
            query = query.Where(p => p.Variants.Any(v => v.OriginalPrice > v.SellingPrice));
        if (search.IsUnlimited.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited == search.IsUnlimited.Value));

        query = search.SortBy switch
        {
            ProductSortOptions.PriceAsc => query.OrderBy(p => p.MinPrice).ThenByDescending(p => p.Id),
            ProductSortOptions.PriceDesc => query.OrderByDescending(p => p.MinPrice).ThenByDescending(p => p.Id),
            ProductSortOptions.NameAsc => query.OrderBy(p => p.Name).ThenByDescending(p => p.Id),
            ProductSortOptions.NameDesc => query.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
            ProductSortOptions.DiscountDesc => query.OrderByDescending(p => p.Variants.Max(v => v.OriginalPrice > 0 ? (1 - v.SellingPrice / v.OriginalPrice) * 100 : 0)).ThenByDescending(p => p.Id),
            ProductSortOptions.DiscountAsc => query.OrderBy(p => p.Variants.Max(v => v.OriginalPrice > 0 ? (1 - v.SellingPrice / v.OriginalPrice) * 100 : 0)).ThenByDescending(p => p.Id),
            ProductSortOptions.Oldest => query.OrderBy(p => p.Id),
            _ => query.OrderByDescending(p => p.Id)
        };

        return query;
    }

    public Task<int> GetProductCountAsync(IQueryable<Domain.Product.Product> query)
    {
        return query.CountAsync();
    }

    public Task<List<Domain.Product.Product>> GetPaginatedProductsAsync(IQueryable<Domain.Product.Product> query, int page, int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<Domain.Product.Product?> GetProductByIdAsync(int id, bool isAdmin = false)
    {
        var query = _context.Set<Domain.Product.Product>()
            .Include(p => p.CategoryGroup.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Images)
            .Include(p => p.Images)
            .AsQueryable();

        if (isAdmin)
        {
            query = query.IgnoreQueryFilters();
        }

        return query.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public Task AddProductAsync(Domain.Product.Product product)
    {
        return _context.Set<Domain.Product.Product>().AddAsync(product).AsTask();
    }

    public void UpdateProduct(Domain.Product.Product product)
    {
        _context.Entry(product).Property("RowVersion").OriginalValue = product.RowVersion;
    }

    public void DeleteProduct(Domain.Product.Product product)
    {
        _context.Set<Domain.Product.Product>().Remove(product);
    }

    public Task<bool> HasOrderHistoryAsync(int productId)
    {
        return _context.Set<Domain.Order.OrderItem>()
            .AnyAsync(oi => _context.Set<Domain.Product.Product>()
                .Where(p => p.Id == productId)
                .SelectMany(p => p.Variants)
                .Select(v => v.Id)
                .Contains(oi.VariantId));
    }

    public Task<List<Domain.Product.ProductVariant>> GetLowStockVariantsAsync(int threshold)
    {
        return _context.Set<Domain.Product.ProductVariant>()
            .Include(v => v.Product.CategoryGroup.Category)
            .Include(v => v.VariantAttributes).ThenInclude(va => va.AttributeValue)
            .Where(v => !v.IsUnlimited && v.Stock <= threshold && v.Stock > 0 && v.IsActive && v.Product.IsActive)
            .OrderBy(v => v.Stock)
            .ToListAsync();
    }

    public Task<int> GetActiveProductCountAsync()
    {
        return _context.Set<Domain.Product.Product>().CountAsync(p => p.IsActive);
    }

    public Task<decimal> GetTotalInventoryValueAsync()
    {
        return _context.Set<Domain.Product.ProductVariant>()
            .Where(p => !p.IsUnlimited && p.Stock > 0 && p.IsActive)
            .SumAsync(p => (decimal)p.Stock * p.PurchasePrice);
    }

    public Task<int> GetOutOfStockCountAsync()
    {
        return _context.Set<Domain.Product.Product>()
            .CountAsync(p => p.IsActive && !p.Variants.Any(v => v.IsUnlimited || v.Stock > 0));
    }

    public Task<int> GetLowStockCountAsync(int threshold)
    {
        return _context.Set<Domain.Product.Product>()
            .CountAsync(p => p.IsActive && p.Variants.Any(v => !v.IsUnlimited && v.Stock > 0 && v.Stock <= threshold));
    }

    public Task<List<Domain.Product.ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds)
    {
        return _context.Set<Domain.Product.ProductVariant>()
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();
    }

    public IQueryable<Domain.Product.ProductVariant> GetDiscountedVariantsQuery(int minDiscount, int maxDiscount, int categoryId)
    {
        var query = _context.Set<Domain.Product.ProductVariant>()
            .Include(v => v.Product.CategoryGroup.Category)
            .Include(v => v.Product.Images)
            .Where(v => v.HasDiscount && v.IsActive && v.Product.IsActive && (v.IsUnlimited || v.Stock > 0))
            .AsQueryable();

        if (categoryId > 0)
            query = query.Where(v => v.Product.CategoryGroup.CategoryId == categoryId);
        if (minDiscount > 0)
            query = query.Where(v => v.DiscountPercentage >= minDiscount);
        if (maxDiscount > 0)
            query = query.Where(v => v.DiscountPercentage <= maxDiscount);

        return query;
    }

    public Task<int> GetDiscountedVariantsCountAsync(IQueryable<Domain.Product.ProductVariant> query)
    {
        return query.CountAsync();
    }

    public Task<List<Domain.Product.ProductVariant>> GetPaginatedDiscountedVariantsAsync(IQueryable<Domain.Product.ProductVariant> query, int page, int pageSize)
    {
        return query
            .OrderByDescending(v => v.DiscountPercentage)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<Domain.Product.ProductVariant?> GetVariantByIdAsync(int id)
    {
        return _context.Set<Domain.Product.ProductVariant>().FindAsync(id).AsTask();
    }

    public Task<int> GetTotalDiscountedVariantsCountAsync()
    {
        return _context.Set<Domain.Product.ProductVariant>().CountAsync(v => v.HasDiscount && v.IsActive);
    }

    public Task<double> GetAverageDiscountPercentageAsync()
    {
        return _context.Set<Domain.Product.ProductVariant>()
            .Where(v => v.HasDiscount && v.IsActive)
            .Select(v => v.DiscountPercentage)
            .DefaultIfEmpty(0)
            .AverageAsync();
    }

    public Task<long> GetTotalDiscountValueAsync()
    {
        return _context.Set<Domain.Product.ProductVariant>()
            .Where(v => v.HasDiscount && !v.IsUnlimited && v.Stock > 0 && v.IsActive)
            .SumAsync(v => (long)(v.OriginalPrice - v.SellingPrice) * v.Stock);
    }

    public Task<List<object>> GetDiscountStatsByCategoryAsync()
    {
        return _context.Set<Domain.Product.ProductVariant>()
            .Include(v => v.Product.CategoryGroup.Category)
            .Where(v => v.HasDiscount && v.IsActive)
            .GroupBy(v => new { v.Product.CategoryGroup.CategoryId, v.Product.CategoryGroup.Category!.Name })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Count = g.Count(),
                AverageDiscount = g.Average(v => v.DiscountPercentage)
            })
            .ToListAsync<object>();
    }
}