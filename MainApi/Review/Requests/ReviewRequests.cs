namespace MainApi.Review.Requests;

public sealed record RejectReviewRequest(string? Reason);

public sealed record ReplyToReviewRequest(string Reply);

public sealed record UpdateReviewStatusRequest(string Status);

public sealed record SubmitReviewRequest(
    int ProductId,
    int? OrderId,
    int Rating,
    string? Title,
    string? Comment
);