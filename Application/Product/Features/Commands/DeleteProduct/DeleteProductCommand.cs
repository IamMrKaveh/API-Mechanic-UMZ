namespace Application.Product.Features.Commands.DeleteProduct;

public record DeleteProductCommand(
    Guid ProductId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "DeleteProduct";

    public string? AuditEntityType => "Product";

    public string? AuditEntityId => ProductId.ToString();
}