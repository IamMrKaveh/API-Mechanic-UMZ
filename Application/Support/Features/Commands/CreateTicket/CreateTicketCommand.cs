using Application.Support.Features.Shared;

namespace Application.Support.Features.Commands.CreateTicket;

public record CreateTicketCommand(
    string Subject,
    string Category,
    string? Priority,
    string Message) : IRequest<ServiceResult<TicketDto>>;