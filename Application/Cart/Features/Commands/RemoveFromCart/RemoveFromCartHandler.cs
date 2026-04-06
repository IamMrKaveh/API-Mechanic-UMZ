using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Common.Interfaces;
using Domain.User.ValueObjects;

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
            cart = await _cartRepository.GetByUserIdAsync(UserId.From(request.UserId.Value), ct);
        else
            cart = await _cartRepository.GetByGuestTokenAsync(request.GuestToken!, ct);

        if (cart is null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        cart.RemoveItem(request.VariantId);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        var cartDetail = await _cartQueryService.GetCartDetailAsync(userId, request.GuestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail ?? new CartDetailDto());
    }
}