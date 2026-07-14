namespace Application.Product.Features.Commands.DeactivateProduct;

public record DeactivateProductCommand(Guid ProductId) : ICommand, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "DeactivateProduct";

    public string? AuditEntityType => "Product";

    public string? AuditEntityId => ProductId.ToString();
}