using Application.Common.Results;
using Domain.Discount.ValueObjects;

namespace Application.Discount.Features.Commands.DeleteDiscount;

public record DeleteDiscountCommand(DiscountCodeId Id) : IRequest<ServiceResult>;