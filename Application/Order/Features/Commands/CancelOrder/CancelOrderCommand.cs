namespace Application.Order.Features.Commands.CancelOrder;

public record CancelOrderCommand(
    Guid OrderId,
    string Reason,
    string? RowVersion)
    : ICommand, IAuditableCommand
{
    public string AuditEventType => "Order";

    public string AuditAction => "CancelOrder";

    public string? AuditEntityType => "Order";

    public string? AuditEntityId => OrderId.ToString();
}
