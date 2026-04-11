using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.UpdateProduct;

namespace Presentation.Product.Mapping;

public static class ProductMappingExtensions
{
    public static CreateProductCommand Enrich(
        this CreateProductCommand command,
        Guid userId)
        => command with
        {
            CreatedByUserId = userId
        };

    public static UpdateProductCommand Enrich(
        this UpdateProductCommand command,
        Guid id,
        Guid userId)
        => command with
        {
            Id = id,
            UpdatedByUserId = userId
        };
}