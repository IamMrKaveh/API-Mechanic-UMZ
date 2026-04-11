using Application.Cart.Features.Shared;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Quartz.Util;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandler(
    ICartRepository cartRepository,
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    ILogger<SyncCartPricesHandler> logger) : IRequestHandler<SyncCartPricesCommand, ServiceResult<SyncCartPricesResult>>
{
    public async Task<ServiceResult<SyncCartPricesResult>> Handle(
        SyncCartPricesCommand request,
        CancellationToken ct)
    {
        Domain.Cart.Aggregates.Cart? cart;
        GuestToken? guestToken;
        UserId? userId;

        if (request.UserId.HasValue)
        {
            userId = UserId.From(request.UserId.Value);
            cart = await cartRepository.FindByUserIdAsync(userId, ct);
        }
        else if (!request.GuestToken.IsNullOrWhiteSpace())
        {
            guestToken = GuestToken.Create(request.GuestToken);
            cart = await cartRepository.FindByGuestTokenAsync(guestToken, ct);
        }
        else
            return ServiceResult<SyncCartPricesResult>.Success(new SyncCartPricesResult { HasChanges = false });

        if (cart is null || cart.IsEmpty)
            return ServiceResult<SyncCartPricesResult>.Success(new SyncCartPricesResult { HasChanges = false });

        var variantIds = cart.Items.Select(i => i.VariantId).ToList();
        var variants = await variantRepository.GetByIdsAsync(variantIds, ct);
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
            cartRepository.Update(cart);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
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