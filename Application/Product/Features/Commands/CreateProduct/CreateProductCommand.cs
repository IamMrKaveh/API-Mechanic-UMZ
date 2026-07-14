using Application.Product.Features.Shared;

namespace Application.Product.Features.Commands.CreateProduct;

public record CreateProductCommand(
    Guid CategoryId,
    Guid BrandId,
    string Name) : ICommand<ProductDetailDto>, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "CreateProduct";

    public string? AuditEntityType => "Product";

    public string? AuditEntityId => null;
}