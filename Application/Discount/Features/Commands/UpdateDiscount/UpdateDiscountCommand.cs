using Application.Discount.Features.Shared;
using Domain.Discount.Enums;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public record UpdateDiscountCommand(
    Guid Id,
    DiscountType DiscountType,
    decimal Value,
    decimal? MaximumDiscountAmount,
    int? UsageLimit,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    bool IsActive) : IRequest<ServiceResult<DiscountDto>>;