namespace Domain.Discount.Enums;

public enum DiscountRestrictionType
{
    MinimumOrderAmount = 1,
    SpecificProduct = 2,
    SpecificCategory = 3,
    SpecificUser = 4,
    FirstOrderOnly = 5,
    MaximumUsagePerUser = 6
}