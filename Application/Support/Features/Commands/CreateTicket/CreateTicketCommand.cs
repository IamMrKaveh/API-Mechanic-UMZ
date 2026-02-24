namespace Application.Support.Features.Commands.CreateTicket;

public sealed record CreateTicketCommand(
    int UserId,
    string Subject,
    string Priority,
    string Message) : IRequest<ServiceResult<int>>;