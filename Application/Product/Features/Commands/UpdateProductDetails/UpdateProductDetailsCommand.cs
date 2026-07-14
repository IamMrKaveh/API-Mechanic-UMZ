namespace Application.Product.Features.Commands.UpdateProductDetails;

public record UpdateProductDetailsCommand(
    Guid ProductId,
    string Name,
    string? Description,
    Guid BrandId,
    bool IsActive,
    string? Sku,
    string RowVersion) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "UpdateProductDetails";

    public string? AuditEntityType => "Product";

    public string? AuditEntityId => ProductId.ToString();
}