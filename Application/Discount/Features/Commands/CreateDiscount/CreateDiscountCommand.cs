namespace Application.Discount.Features.Commands.CreateDiscount;

public record CreateDiscountCommand : IRequest<ServiceResult<DiscountCodeDto>>
{
    public required string Code { get; init; }
    public decimal Percentage { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int? MaxUsagePerUser { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? StartsAt { get; init; }
    public List<CreateDiscountRestrictionDto>? Restrictions { get; init; }
}