using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Cart.Features.Commands.ClearCart;

public class ClearCartHandler(
    ICartRepository cartRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearCartCommand, ServiceResult>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(ClearCartCommand request, CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;
        if (request.UserId.HasValue)
            cart = await _cartRepository.FindByUserIdAsync(UserId.From(request.UserId.Value), ct);
        else
            cart = await _cartRepository.FindByGuestTokenAsync(GuestToken.Create(request.GuestToken!), ct);

        if (cart is null)
            return ServiceResult.Success();

        cart.Clear();
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}