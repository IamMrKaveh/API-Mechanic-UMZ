namespace Application.Product.Features.Commands.ActivateProduct;

public record ActivateProductCommand(Guid ProductId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "ActivateProduct";

    public string? AuditEntityType => "Product";

    public string? AuditEntityId => ProductId.ToString();
}