namespace Application.Discount.Features.Commands.UpdateDiscount;

public record UpdateDiscountCommand : IRequest<ServiceResult>
{
    public int Id { get; init; }
    public decimal Percentage { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal? MinOrderAmount { get; init; }
    public int? UsageLimit { get; init; }
    public int? MaxUsagePerUser { get; init; }
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime? StartsAt { get; init; }
    public required string ConcurrencyToken { get; init; }
}