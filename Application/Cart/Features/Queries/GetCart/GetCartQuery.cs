namespace Application.Cart.Features.Queries.GetCart;

public record GetCartQuery : IRequest<ServiceResult<CartDetailDto>>;