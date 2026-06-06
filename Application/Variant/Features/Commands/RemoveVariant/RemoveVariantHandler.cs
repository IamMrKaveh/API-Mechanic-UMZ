using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantHandler(
    IVariantRepository variantRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<RemoveVariantCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RemoveVariantCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId.Value);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        try
        {
            variant.Remove(userId.Value);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        variantRepository.Update(variant);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            variant.ProductId,
            "RemoveVariant",
            $"واریانت {variantId.Value} حذف شد.",
            userId);

        return ServiceResult.Success();
    }
}