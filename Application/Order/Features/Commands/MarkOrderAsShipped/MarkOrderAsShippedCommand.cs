namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public record MarkOrderAsShippedCommand(
    Guid OrderId,
    string? RowVersion)
    : ICommand;
