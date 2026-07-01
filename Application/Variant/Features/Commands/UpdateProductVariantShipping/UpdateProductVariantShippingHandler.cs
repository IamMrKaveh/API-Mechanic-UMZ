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
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<UpdateVariantShippingCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateVariantShippingCommand request,
        CancellationToken ct)
    {
        if (currentUserService.UserId is null)
            return ServiceResult.Unauthorized();

        var userId = UserId.From(currentUserService.UserId.Value);
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

        var existingDimensions = variant.Shippings
            .Where(s => s.Width > 0 || s.Height > 0 || s.Length > 0)
            .Select(s => new { s.Width, s.Height, s.Length })
            .FirstOrDefault();

        var width = existingDimensions?.Width ?? 0m;
        var height = existingDimensions?.Height ?? 0m;
        var length = existingDimensions?.Length ?? 0m;

        var assignments = newShippings.Select(s =>
            new ShippingAssignment(s.Id, request.WeightGrams, width, height, length));

        variant.SetShippingMethods(
            request.ShippingMultiplier,
            assignments);

        variantRepository.Update(variant);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variantId,
            "UpdateVariantShippings",
            $"روش‌های ارسال و وزن ({request.WeightGrams} گرم) واریانت {variantId.Value} به‌روزرسانی شد.",
            userId);

        return ServiceResult.Success();
    }
}