using Application.Support.Features.Shared;

namespace Application.Support.Features.Commands.CreateTicket;

public record CreateTicketCommand(
    Guid UserId,
    string Subject,
    string Category,
    string? Priority,
    string InitialMessage) : IRequest<ServiceResult<TicketDto>>;