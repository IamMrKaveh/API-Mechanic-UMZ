using Application.Cart.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Cart.Features.Commands.RemoveFromCart;

public class RemoveFromCartHandler(
    ICartRepository cartRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveFromCartCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<CartDetailDto>> Handle(
        RemoveFromCartCommand request,
        CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;
        if (request.UserId.HasValue)
            cart = await _cartRepository.FindByUserIdAsync(UserId.From(request.UserId.Value), ct);
        else
            cart = await _cartRepository.FindByGuestTokenAsync(GuestToken.Create(request.GuestToken!), ct);

        if (cart is null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        var variantId = VariantId.From(request.VariantId);
        cart.RemoveItem(variantId);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;

        var guestToken = GuestToken.Create(request.GuestToken);

        var cartDetail = await _cartQueryService.GetCartDetailAsync(userId, guestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail ?? new CartDetailDto());
    }
}