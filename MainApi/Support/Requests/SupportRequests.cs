namespace Presentation.Support.Requests;

public record CreateTicketRequest(
    string Subject,
    string Category,
    string Message,
    string Priority = "Normal"
);

public record ReplyToTicketRequest(string Message);