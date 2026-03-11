namespace Application.Support.Features.Commands.CreateTicket;

public sealed record CreateTicketCommand(string Subject, string Message) : IRequest<ServiceResult<int>>;