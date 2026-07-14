namespace Application.Product.Features.Commands.RestoreProduct;

public record RestoreProductCommand(Guid ProductId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "RestoreProduct";

    public string? AuditEntityType => "Product";

    public string? AuditEntityId => ProductId.ToString();
}