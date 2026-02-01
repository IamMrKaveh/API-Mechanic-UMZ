namespace Application.Features.Admin.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<ServiceResult<AdminProductViewDto>>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int CategoryGroupId { get; init; }
    public bool IsActive { get; init; } = true; public string? Sku { get; init; }
    public string VariantsJson { get; init; } = "[]"; public List<FileDto>? Images { get; init; }
    public int? PrimaryImageIndex { get; init; }
}