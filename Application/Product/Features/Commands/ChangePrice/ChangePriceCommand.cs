namespace Application.Product.Features.Commands.ChangePrice;

public record ChangePriceCommand(
    Guid ProductId,
    Guid VariantId,
    Guid UserId,
    decimal SellingPrice,
    decimal OriginalPrice)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "Product";

    public string AuditAction => "ChangePrice";

    public string? AuditEntityType => "Variant";

    public string? AuditEntityId => VariantId.ToString();
}