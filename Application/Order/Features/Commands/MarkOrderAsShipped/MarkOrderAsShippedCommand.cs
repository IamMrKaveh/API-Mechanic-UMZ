namespace Application.Order.Features.Commands.MarkOrderAsShipped;

public record MarkOrderAsShippedCommand(Guid OrderId) : IRequest<ServiceResult>;