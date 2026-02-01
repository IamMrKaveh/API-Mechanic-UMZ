namespace Application.Features.Admin.Products.Commands.UpdateProduct;

public record UpdateProductCommand : IRequest<ServiceResult<AdminProductViewDto>>
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int CategoryGroupId { get; init; }
    public bool IsActive { get; init; }
    public string? Sku { get; init; }
    public required string RowVersion { get; init; }
    public string VariantsJson { get; init; } = "[]";
    public List<FileDto>? Images { get; init; }
    public int? PrimaryImageIndex { get; init; }
    public List<int>? DeletedMediaIds { get; init; }
}