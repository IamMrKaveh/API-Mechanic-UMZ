namespace Application.Support.Features.Commands.ReplyToTicket;

public record ReplyToTicketCommand(
    Guid TicketId,
    string Content)
    : ICommand;