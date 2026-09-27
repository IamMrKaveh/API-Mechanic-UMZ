using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.RemoveVariant;

public class RemoveVariantHandler(
    IVariantRepository variantRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService)
    : ICommandHandler<RemoveVariantCommand>
{
    public async Task<ServiceResult> Handle(
        RemoveVariantCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId!.Value);

        var variantResult = await (variantRepository.GetByIdAsync(variantId, ct)).OrNotFoundAsync("واریانت یافت نشد.");
        if (variantResult.IsFailure) return variantResult.Error;
        var variant = variantResult.Value;

        try
        {
            variant.Remove(userId.Value);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        variantRepository.Update(variant);

        await auditService.LogProductEventAsync(
            variant.ProductId,
            "RemoveVariant",
            $"واریانت {variantId.Value} حذف شد.",
            userId);

        return ServiceResult.Success();
    }
}
