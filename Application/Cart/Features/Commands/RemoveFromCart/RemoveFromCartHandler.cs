using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
using Domain.Cart.Interfaces;
using Domain.Common.Interfaces;
using SharedKernel.Contracts;

namespace Application.Cart.Features.Commands.RemoveFromCart;

public class RemoveFromCartHandler(
    ICartRepository cartRepository,
    ICartQueryService cartQueryService,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveFromCartCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult<CartDetailDto>> Handle(
        RemoveFromCartCommand request,
        CancellationToken ct)
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.CurrentUser.UserId, _currentUser.GuestId, ct);
        if (cart == null)
            return ServiceResult<CartDetailDto>.NotFound("سبد خرید یافت نشد.");

        cart.RemoveItem(request.VariantId);
        await _unitOfWork.SaveChangesAsync(ct);

        var cartDetail = await _cartQueryService.GetCartDetailAsync(
            _currentUser.CurrentUser.UserId, _currentUser.GuestId, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}