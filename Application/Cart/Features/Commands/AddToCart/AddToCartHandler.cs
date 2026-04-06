using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Application.Common.Results;
using Domain.Cart.Aggregates;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Common.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;

namespace Application.Cart.Features.Commands.AddToCart;

public class AddToCartHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    ICartQueryService cartQueryService,
    IUnitOfWork unitOfWork,
    ILogger<AddToCartHandler> logger) : IRequestHandler<AddToCartCommand, ServiceResult<CartDetailDto>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly ICartQueryService _cartQueryService = cartQueryService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<AddToCartHandler> _logger = logger;

    public async Task<ServiceResult<CartDetailDto>> Handle(
        AddToCartCommand request,
        CancellationToken ct)
    {
        var variant = await _variantRepository.GetByIdAsync(request.VariantId, ct);
        if (variant is null || variant.IsDeleted || !variant.IsActive)
            return ServiceResult<CartDetailDto>.NotFound("محصول یافت نشد یا فعال نیست.");

        if (!variant.IsUnlimited && variant.AvailableStock < request.Quantity)
            return ServiceResult<CartDetailDto>.Validation($"موجودی کافی نیست. موجود: {variant.AvailableStock}");

        Cart? cart;
        if (request.UserId.HasValue)
        {
            cart = await _cartRepository.GetByUserIdAsync(UserId.From(request.UserId.Value), ct);
            if (cart is null)
            {
                cart = Cart.CreateForUser(UserId.From(request.UserId.Value));
                await _cartRepository.AddAsync(cart, ct);
            }
        }
        else
        {
            var guestToken = GuestToken.Create(request.GuestToken!);
            cart = await _cartRepository.GetByGuestTokenAsync(guestToken.Value, ct);
            if (cart is null)
            {
                cart = Cart.CreateForGuest(guestToken);
                await _cartRepository.AddAsync(cart, ct);
            }
        }

        cart.AddItem(request.VariantId, request.Quantity, variant.SellingPrice.Amount);
        _cartRepository.Update(cart);
        await _unitOfWork.SaveChangesAsync(ct);

        UserId? userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;
        var cartDetail = await _cartQueryService.GetCartDetailAsync(userId, request.GuestToken, ct);

        return ServiceResult<CartDetailDto>.Success(cartDetail!);
    }
}