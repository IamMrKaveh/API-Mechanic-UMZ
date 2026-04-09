using Application.Common.Results;
using Application.Discount.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Discount.Features.Commands.ApplyDiscount;

public record ApplyDiscountCommand(
    string Code,
    decimal OrderAmount,
    UserId UserId,
    OrderId OrderId) : IRequest<ServiceResult<DiscountApplicationResult>>;