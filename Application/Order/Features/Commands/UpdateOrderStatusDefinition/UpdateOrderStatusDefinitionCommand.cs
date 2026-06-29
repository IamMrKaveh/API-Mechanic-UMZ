namespace Application.Order.Features.Commands.UpdateOrderStatusDefinition;

public record UpdateOrderStatusDefinitionCommand(
    Guid Id,
    string DisplayName,
    string? Icon,
    string? Color,
    int SortOrder,
    bool AllowCancel,
    bool AllowEdit,
    string? RowVersion = null)
    : ICommand;