namespace Application.Order.Features.Commands.CreateOrderStatus;

public record CreateOrderStatusCommand(
    string Name,
    string DisplayName,
    string? Icon,
    string? Color,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit
    ) : IRequest<ServiceResult<OrderStatusDto>>;