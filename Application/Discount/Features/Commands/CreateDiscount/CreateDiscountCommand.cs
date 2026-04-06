using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Discount.Enums;

namespace Application.Discount.Features.Commands.CreateDiscount;

public record CreateDiscountCommand(
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaximumDiscountAmount,
    int? UsageLimit,
    DateTime? StartsAt,
    DateTime? ExpiresAt) : IRequest<ServiceResult<DiscountDto>>;