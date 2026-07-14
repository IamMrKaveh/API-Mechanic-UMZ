using Application.Order.Features.Shared;

namespace Application.Order.Features.Commands.CreateOrderStatus;

public record CreateOrderStatusCommand(
    string Name,
    string DisplayName,
    string? Icon,
    string? Color,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit)
    : ICommand<OrderStatusDto>, IAuditableCommand
{
    public string AuditEventType => "OrderStatus";

    public string AuditAction => "CreateOrderStatus";

    public string? AuditEntityType => "OrderStatus";

    public string? AuditEntityId => null;
}