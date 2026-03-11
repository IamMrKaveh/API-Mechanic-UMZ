using Application.Common.Models;

namespace Application.Cart.Features.Queries.GetCartSummary;

public record GetCartSummaryQuery : IRequest<ServiceResult<CartSummaryDto>>;