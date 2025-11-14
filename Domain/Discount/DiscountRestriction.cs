namespace Domain.Discount;

public class DiscountRestriction
{
    public int Id { get; set; }

    public int DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; } = null!;

    public required string RestrictionType { get; set; }

    public int? EntityId { get; set; }
}