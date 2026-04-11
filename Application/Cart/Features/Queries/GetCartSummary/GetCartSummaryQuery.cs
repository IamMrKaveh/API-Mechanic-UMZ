using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Queries.GetCartSummary;

public record GetCartSummaryQuery(
    Guid? UserId,
    string? GuestToken) : IRequest<ServiceResult<CartSummaryDto>>;