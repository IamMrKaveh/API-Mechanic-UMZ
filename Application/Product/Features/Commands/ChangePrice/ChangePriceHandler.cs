using Domain.Product.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.ChangePrice;

public sealed class ChangePriceHandler(
    IVariantRepository variantRepository)
    : ICommandHandler<ChangePriceCommand>
{
    public async Task<ServiceResult> Handle(
        ChangePriceCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var productId = ProductId.From(request.ProductId);

        var variantResult = await variantRepository.GetByIdAsync(variantId, ct).OrNotFoundAsync(v => v.ProductId != productId, "واریانت یافت نشد.");
        if (variantResult.IsFailure) return variantResult.ToServiceResult();
        var variant = variantResult.Value;

        try
        {
            var newPrice = Money.Create(request.SellingPrice);
            var compareAtPrice = request.OriginalPrice > request.SellingPrice
                ? Money.Create(request.OriginalPrice)
                : null;

            variant.ChangePrice(newPrice, compareAtPrice);
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }

        variantRepository.Update(variant);


        return ServiceResult.Success();
    }
}