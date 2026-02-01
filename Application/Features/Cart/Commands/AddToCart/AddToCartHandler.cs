namespace Application.Features.Cart.Commands.AddToCart;

public class AddToCartHandler : IRequestHandler<AddToCartCommand, ServiceResult<CartDto>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AddToCartHandler(ICartRepository cartRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _cartRepository = cartRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResult<CartDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetCartEntityAsync(request.UserId, request.GuestId);

        if (cart == null)
        {
            cart = new Domain.Cart.Cart
            {
                UserId = request.UserId,
                GuestToken = request.UserId.HasValue ? null : request.GuestId,
                CreatedAt = DateTime.UtcNow
            };
            await _cartRepository.AddCartAsync(cart);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var variant = await _cartRepository.GetVariantByIdAsync(request.Dto.VariantId);
        if (variant == null) return ServiceResult<CartDto>.Fail("Variant not found", 404);

        if (!variant.IsUnlimited && variant.StockQuantity < request.Dto.Quantity)
            return ServiceResult<CartDto>.Fail("Insufficient stock", 409);

        var cartItem = await _cartRepository.GetCartItemAsync(cart.Id, request.Dto.VariantId);

        if (cartItem != null)
        {
            if (request.Dto.CartItemRowVersion != null)
                _cartRepository.SetCartItemRowVersion(cartItem, request.Dto.CartItemRowVersion);

            cartItem.Quantity += request.Dto.Quantity;
            _cartRepository.UpdateCartItem(cartItem);
        }
        else
        {
            cartItem = new CartItem
            {
                CartId = cart.Id,
                VariantId = request.Dto.VariantId,
                Quantity = request.Dto.Quantity
            };
            await _cartRepository.AddCartItemAsync(cartItem);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            // Fetch updated cart DTO logic here or reuse a query handler/service logic
            // For brevity, assuming mapping logic exists or a separate query is called
            var updatedCart = await _cartRepository.GetCartAsync(request.UserId, request.GuestId);
            return ServiceResult<CartDto>.Ok(_mapper.Map<CartDto>(updatedCart));
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<CartDto>.Fail("Cart modified by another user", 409);
        }
    }
}