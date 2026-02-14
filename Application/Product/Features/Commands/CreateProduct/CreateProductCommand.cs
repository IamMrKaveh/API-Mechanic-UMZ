namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand : IRequest<ServiceResult<AdminProductDetailDto>>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int CategoryGroupId { get; init; }
    public bool IsActive { get; init; } = true;
    public string? Sku { get; init; }
    public List<CreateProductVariantInput> Variants { get; init; } = new();
    public List<FileDto>? Images { get; init; }
    public int? PrimaryImageIndex { get; init; }
}