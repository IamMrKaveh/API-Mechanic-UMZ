namespace Presentation.Support.Requests;

public record CreateTicketRequest(
    string Subject,
    string Category,
    string Priority,
    string Message
);

public record ReplyToTicketRequest(string Message);