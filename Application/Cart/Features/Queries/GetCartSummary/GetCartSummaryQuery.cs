using Application.Cart.Features.Shared;

namespace Application.Cart.Features.Queries.GetCartSummary;

public record GetCartSummaryQuery() : IQuery<CartSummaryDto>;