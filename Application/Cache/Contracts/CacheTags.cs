namespace Application.Cache.Contracts;

/// <summary>
/// Tags for <c>[RequestOutputCache]</c> / <c>[RequestOutputCacheEvict]</c>.
/// Each value is the EF entity CLR type name, so the EF Core auto-evict interceptor
/// invalidates the tagged responses whenever that entity is saved.
/// A cached query must list every entity its response is built from.
/// </summary>
public static class CacheTags
{
    public const string Product = nameof(global::Domain.Product.Aggregates.Product);
    public const string ProductVariant = nameof(global::Domain.Variant.Aggregates.ProductVariant);
    public const string VariantShipping = nameof(global::Domain.Variant.Entities.VariantShipping);
    public const string Category = nameof(global::Domain.Category.Aggregates.Category);
    public const string Brand = nameof(global::Domain.Brand.Aggregates.Brand);
    public const string Media = nameof(global::Domain.Media.Aggregates.Media);
    public const string Inventory = nameof(global::Domain.Inventory.Aggregates.Inventory);
    public const string Warehouse = nameof(global::Domain.Inventory.Aggregates.Warehouse);
    public const string Shipping = nameof(global::Domain.Shipping.Aggregates.Shipping);
    public const string PaymentMethod = nameof(global::Domain.Payment.Aggregates.PaymentMethod);
    public const string Order = nameof(global::Domain.Order.Aggregates.Order);
    public const string OrderItem = nameof(global::Domain.Order.Entities.OrderItem);
    public const string OrderStatus = nameof(global::Domain.Order.Entities.OrderStatus);
    public const string ProductReview = nameof(global::Domain.Review.Aggregates.ProductReview);
    public const string AttributeType = nameof(global::Domain.Attribute.Aggregates.AttributeType);
    public const string AttributeValue = nameof(global::Domain.Attribute.Entities.AttributeValue);

    /// <summary>
    /// Tags that do not map to an EF entity. Responses carrying only these tags expire by TTL
    /// (reporting and static reference data) or are evicted explicitly through
    /// <c>IRequestOutputCacheInvalidator</c>.
    /// </summary>
    public static class Manual
    {
        public const string Analytics = "analytics";
        public const string Location = "location";

        public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
        {
            Analytics,
            Location
        };
    }
}
