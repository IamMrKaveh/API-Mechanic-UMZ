namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public record UpdateOrderStatusDefinitionCommand(
    Guid Id,
    string DisplayName,
    string? Icon,
    string? Color,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit,
    string? RowVersion)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "OrderStatus";

    public string AuditAction => "UpdateOrderStatusDefinition";

    public string? AuditEntityType => "OrderStatus";

    public string? AuditEntityId => Id.ToString();
}
