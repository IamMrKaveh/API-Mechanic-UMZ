namespace Presentation.Support.Requests;

public record CreateTicketRequest(
    string Subject,
    string Priority,
    string Message
);

public record ReplyToTicketRequest(string Message);