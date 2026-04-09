using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork,
    ILogger<SyncCartPricesHandler> logger) : IRequestHandler<SyncCartPricesCommand, ServiceResult<SyncCartPricesResult>>
{
    private readonly ICartRepository _cartRepository = cartRepository;
    private readonly IVariantRepository _variantRepository = variantRepository;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<SyncCartPricesHandler> _logger = logger;

    public async Task<ServiceResult<SyncCartPricesResult>> Handle(
        SyncCartPricesCommand request,
        CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;
        if (_currentUser.UserId is not null)
            cart = await _cartRepository.FindByUserIdAsync(UserId.From(_currentUser.UserId.Value), ct);
        else if (_currentUser.GuestToken is not null)
            cart = await _cartRepository.FindByGuestTokenAsync(GuestToken.Create(_currentUser.GuestToken), ct);
        else
            return ServiceResult<SyncCartPricesResult>.Success(new SyncCartPricesResult { HasChanges = false });

        if (cart is null || cart.IsEmpty)
            return ServiceResult<SyncCartPricesResult>.Success(new SyncCartPricesResult { HasChanges = false });

        var variantIds = cart.Items.Select(i => i.VariantId).ToList();
        var variants = await _variantRepository.GetByIdsAsync(variantIds, ct);
        var variantLookup = variants.ToDictionary(v => v.Id);

        var priceChanges = new List<CartPriceChangeDto>();
        var removedVariantIds = new List<Guid>();

        foreach (var item in cart.Items.ToList())
        {
            if (!variantLookup.TryGetValue(item.VariantId, out var variant)
                || !variant.IsActive
                || variant.IsDeleted)
            {
                cart.RemoveItem(item.VariantId);
                removedVariantIds.Add(item.VariantId.Value);
                continue;
            }

            var currentUnitPrice = variant.Price;
            var currentOriginalPrice = variant.CompareAtPrice ?? variant.Price;

            if (item.UnitPrice.Amount != currentUnitPrice.Amount)
            {
                priceChanges.Add(new CartPriceChangeDto(
                    item.VariantId.Value,
                    item.ProductName.Value,
                    item.UnitPrice.Amount,
                    currentUnitPrice.Amount));

                cart.RefreshItemPrice(item.VariantId, currentUnitPrice, currentOriginalPrice);
            }
        }

        var hasChanges = priceChanges.Count > 0 || removedVariantIds.Count > 0;

        if (hasChanges)
        {
            _cartRepository.Update(cart);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "قیمت‌های سبد {CartId} همگام‌سازی شد. تغییر قیمت: {PriceChangeCount}، حذف شده: {RemovedCount}",
                cart.Id, priceChanges.Count, removedVariantIds.Count);
        }

        return ServiceResult<SyncCartPricesResult>.Success(new SyncCartPricesResult
        {
            HasChanges = hasChanges,
            PriceChanges = priceChanges,
            RemovedVariantIds = removedVariantIds
        });
    }
}