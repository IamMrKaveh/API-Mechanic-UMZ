using Application.Common.Results;

namespace Application.Cart.Features.Queries.GetCartSummary;

public record GetCartSummaryQuery : IRequest<ServiceResult<CartSummaryDto>>;