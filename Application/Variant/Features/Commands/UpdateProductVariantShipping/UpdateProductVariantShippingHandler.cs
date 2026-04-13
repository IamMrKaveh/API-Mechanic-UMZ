using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
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

        var variant = await variantRepository.GetVariantWithShippingsAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        var allShippings = await shippingRepository.GetAllAsync(false, ct);
        var validIds = allShippings.Select(s => s.Id.Value).ToHashSet();

        var invalidIds = request.EnabledShippingIds.Where(id => !validIds.Contains(id)).ToList();
        if (invalidIds.Count != 0)
            return ServiceResult.Failure($"روش‌های ارسال نامعتبر: {string.Join(", ", invalidIds)}");

        var newShippingIds = request.EnabledShippingIds.Select(ShippingId.From).ToList();

        var newShippings = await shippingRepository.GetByIdsAsync(newShippingIds, ct);
        var assignments = newShippings.Select(s =>
            new ShippingAssignment(s.Id, 0, 0, 0, 0));

        variant.SetShippingMethods(assignments);

        variantRepository.Update(variant);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variantId,
            "UpdateVariantShippings",
            $"روش‌های ارسال واریانت {variantId.Value} به‌روزرسانی شد.",
            userId);

        return ServiceResult.Success();
    }
}