using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Media.Features.Shared;
using Application.Product.Features.Shared;
using Application.Shipping.Features.Shared;
using Application.Variant.Contracts;
using Domain.Shipping.Interfaces;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Variant.QueryServices;

public class VariantQueryService(DBContext context, IShippingRepository shippingRepository) : IVariantQueryService
{
    private readonly DBContext _context = context;
    private readonly IShippingRepository _shippingRepository = shippingRepository;

    public async Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        int productId,
        bool activeOnly,
        CancellationToken ct = default)
    {
        var query = _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.ProductVariantShippings)
            .Where(v => v.ProductId == productId && !v.IsDeleted);

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        var variants = await query.ToListAsync(ct);

        return variants.Select(v => new ProductVariantViewDto
        {
            Id = v.Id,
            Sku = v.Sku?.Value,
            PurchasePrice = v.PurchasePrice.Amount,
            SellingPrice = v.SellingPrice.Amount,
            OriginalPrice = v.OriginalPrice.Amount,
            Stock = v.StockQuantity,
            IsUnlimited = v.IsUnlimited,
            IsActive = v.IsActive,
            IsInStock = v.IsInStock,
            HasDiscount = v.HasDiscount,
            DiscountPercentage = v.DiscountPercentage,
            ShippingMultiplier = v.ShippingMultiplier,
            RowVersion = v.RowVersion != null ? Convert.ToBase64String(v.RowVersion) : null,
            EnabledShippingIds = v.ProductVariantShippings
                .Where(pvsm => pvsm.IsActive)
                .Select(pvsm => pvsm.ShippingId)
                .ToList(),
            Attributes = v.VariantAttributes
                .Where(va => va.AttributeValue?.AttributeType != null)
                .ToDictionary(
                    va => va.AttributeValue!.AttributeType!.Name,
                    va => new AttributeValueDto(
                        va.AttributeValue!.Id,
                        va.AttributeValue.AttributeType!.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue,
                        va.AttributeValue.HexCode)),
            Images = new List<MediaDto>()
        });
    }

    public async Task<ProductVariantShippingInfoDto?> GetVariantShippingInfoAsync(
        int variantId,
        CancellationToken ct = default)
    {
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.ProductVariantShippings)
            .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, ct);

        if (variant == null) return null;

        var allShippings = await _shippingRepository.GetAllAsync(false, ct);

        var enabledIds = variant.ProductVariantShippings
            .Where(pvsm => pvsm.IsActive)
            .Select(pvsm => pvsm.ShippingId)
            .ToHashSet();

        return new ProductVariantShippingInfoDto
        {
            VariantId = variant.Id,
            ProductName = variant.Product?.Name,
            VariantDisplayName = variant.Sku?.Value ?? "N/A",
            ShippingMultiplier = variant.ShippingMultiplier,
            AvailableShippings = allShippings.Select(sm => new ShippingSelectionDto
            {
                ShippingId = sm.Id,
                Name = sm.Name,
                BaseCost = sm.Cost,
                Description = sm.Description,
                IsEnabled = enabledIds.Contains(sm.Id)
            }).ToList()
        };
    }

    public async Task<VariantAvailabilityDto?> GetVariantAvailabilityAsync(
        int variantId,
        CancellationToken ct = default)
    {
        var variant = await _context.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, ct);

        if (variant == null) return null;

        return new VariantAvailabilityDto
        {
            VariantId = variant.Id,
            OnHand = variant.StockQuantity,
            Reserved = variant.ReservedQuantity,
            Available = variant.AvailableStock,
            IsInStock = variant.IsInStock,
            IsUnlimited = variant.IsUnlimited,
            LastUpdated = DateTime.UtcNow
        };
    }
}