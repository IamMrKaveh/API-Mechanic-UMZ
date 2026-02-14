namespace Domain.Discount;

public class DiscountRestriction : BaseEntity
{
    public int DiscountCodeId { get; private set; }
    public DiscountRestrictionType Type { get; private set; }
    public int? EntityId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public DiscountCode DiscountCode { get; private set; } = null!;

    private DiscountRestriction()
    { }

    internal static DiscountRestriction Create(int discountCodeId, DiscountRestrictionType type, int? entityId)
    {
        return new DiscountRestriction
        {
            DiscountCodeId = discountCodeId,
            Type = type,
            EntityId = entityId,
            CreatedAt = DateTime.UtcNow
        };
    }

    #region Query Methods

    public bool IsUserRestriction() => Type == DiscountRestrictionType.User;

    public bool IsCategoryRestriction() => Type == DiscountRestrictionType.Category;

    public bool IsProductRestriction() => Type == DiscountRestrictionType.Product;

    public bool IsBrandRestriction() => Type == DiscountRestrictionType.Brand;

    public bool AppliesToEntity(int entityId) => EntityId == entityId;

    #endregion Query Methods
}