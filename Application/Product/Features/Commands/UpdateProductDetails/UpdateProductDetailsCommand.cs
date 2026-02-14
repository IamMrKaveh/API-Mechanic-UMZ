namespace Application.Product.Features.Commands.UpdateProductDetails;

public record UpdateProductDetailsCommand : IRequest<ServiceResult>
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int CategoryGroupId { get; init; }
    public bool IsActive { get; init; }
    public string? Sku { get; init; }
    public required string RowVersion { get; init; }
}