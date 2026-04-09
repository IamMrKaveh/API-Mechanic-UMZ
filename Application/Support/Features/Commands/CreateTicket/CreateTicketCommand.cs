using Application.Common.Results;
using Application.Support.Features.Shared;

namespace Application.Support.Features.Commands.CreateTicket;

public record CreateTicketCommand(
    Guid UserId,
    string Subject,
    string? Priority,
    string InitialMessage) : IRequest<ServiceResult<TicketDto>>;