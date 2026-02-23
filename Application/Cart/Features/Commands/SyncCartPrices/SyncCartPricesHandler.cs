using AngleSharp.Common;

namespace Application.Cart.Features.Commands.SyncCartPrices;

public class SyncCartPricesHandler : IRequestHandler<SyncCartPricesCommand, ServiceResult<SyncCartPricesResult>>
{
    private readonly ICartRepository _cartRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncCartPricesHandler> _logger;

    public SyncCartPricesHandler(
        ICartRepository cartRepository,
        IProductRepository productRepository,
        ICurrentUserService currentUser,
        IUnitOfWork unitOfWork,
        ILogger<SyncCartPricesHandler> logger
        )
    {
        _cartRepository = cartRepository;
        _productRepository = productRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<SyncCartPricesResult>> Handle(
        SyncCartPricesCommand request,
        CancellationToken ct
        )
    {
        var cart = await _cartRepository.GetCartAsync(
            _currentUser.UserId, _currentUser.GuestId, ct);

        if (cart == null || cart.IsEmpty)
            return ServiceResult<SyncCartPricesResult>.Success(new SyncCartPricesResult { HasChanges = false });

        var variantIds = cart.GetVariantIds().ToList();
        var variants = await _productRepository.GetVariantsByIdsAsync(variantIds, ct);
        var variantLookup = variants.ToDictionary(v => v.Id);
        var priceChanges = new List<CartPriceChangeDto>();
        var removedVariantIds = new List<int>();

        foreach (var item in cart.CartItems.ToList())
        {
            if (!variantLookup.TryGetValue(item.VariantId, out var variant)
                || !variant.IsActive || variant.IsDeleted)
            {
                // واریانت دیگر موجود نیست، حذف از سبد
                cart.RemoveItem(item.VariantId);
                removedVariantIds.Add(item.VariantId);
                continue;
            }

            if (item.SellingPrice != variant.SellingPrice)
            {
                priceChanges.Add(new CartPriceChangeDto(
                    item.VariantId,
                    variant.Product?.Name ?? "نامشخص",
                    item.SellingPrice,
                    variant.SellingPrice));

                cart.UpdateItemPrice(item.VariantId, variant.SellingPrice);
            }
        }

        var hasChanges = priceChanges.Count > 0 || removedVariantIds.Count > 0;

        if (hasChanges)
        {
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