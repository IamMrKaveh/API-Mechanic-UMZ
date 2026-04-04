using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Common.ValueObjects;
using Domain.Discount.Aggregates;

namespace Application.Discount.Features.Commands.CreateDiscount;

public record CreateDiscountCommand(
    DiscountCode Code,
    Percentage Percentage,
    decimal? MaxDiscountAmount,
    decimal? MinOrderAmount,
    int? UsageLimit,
    int? MaxUsagePerUser,
    DateTime? ExpiresAt,
    DateTime? StartsAt,
    List<CreateDiscountRestrictionDto>? Restrictions) : IRequest<ServiceResult<DiscountCodeDto>>;