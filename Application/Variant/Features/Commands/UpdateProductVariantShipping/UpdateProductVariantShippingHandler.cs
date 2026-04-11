using Domain.Shipping.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public class UpdateProductVariantShippingHandler(
    IVariantRepository variantRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateVariantShippingCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateVariantShippingCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var variantId = VariantId.From(request.VariantId);

        var variant = await variantRepository.GetByIdForUpdateAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("محصول یافت نشد.");

        var validShippingIds = await shippingRepository.GetAllAsync(false, ct);
        var validIds = validShippingIds.Select(sm => sm.Id).ToHashSet();

        var invalidIds = request.EnabledShippingIds.Where(id => !validIds.Contains(id)).ToList();
        if (invalidIds.Any())
        {
            return ServiceResult.Failure($"برخی از روش‌های ارسال انتخاب شده نامعتبر هستند: {string.Join(", ", invalidIds)}");
        }

        variant.UpdateDetails(variant.Sku, request.ShippingMultiplier);

        var existings = variant.VariantShippings.ToList();
        var sToRemove = existings
            .Where(x => !request.EnabledShippingIds.Contains(x.ShippingId))
            .ToList();

        foreach (var item in sToRemove)
        {
            variant.RemoveShipping(item.ShippingId);
        }

        var existingIds = existings.Select(x => x.ShippingId).ToHashSet();
        var idsToAdd = request.EnabledShippingIds.Where(id => !existingIds.Contains(id)).ToList();

        if (idsToAdd.Count != 0)
        {
            var shippingsToAdd = await shippingRepository.GetByIdsAsync(idsToAdd, ct);
            foreach (var sm in shippingsToAdd)
            {
                variant.AddShipping(sm);
            }
        }

        variantRepository.Update(variant);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            variant.ProductId,
            "UpdateShippings",
            $"Shipping s updated for variant {variantId}.",
            userId);

        return ServiceResult.Success();
    }
}