using Application.Security.Contracts;

namespace Application.Cart.Features.Commands.ClearCart;

public class ClearCartHandler : IRequestHandler<ClearCartCommand, ServiceResult>
{
    private readonly ICartRepository _cartRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public ClearCartHandler(
        ICartRepository cartRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork)
    {
        _cartRepository = cartRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.UserId, _currentUser.GuestId, cancellationToken);

        if (cart != null)
        {
            cart.Clear();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ServiceResult.Success();
    }
}