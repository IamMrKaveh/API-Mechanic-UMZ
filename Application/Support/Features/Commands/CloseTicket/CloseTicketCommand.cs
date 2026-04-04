using Application.Common.Results;
using Domain.Support.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Support.Features.Commands.CloseTicket;

public sealed record CloseTicketCommand(
    TicketId TicketId,
    UserId UserId,
    bool IsAdmin) : IRequest<ServiceResult<bool>>;