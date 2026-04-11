namespace Presentation.Review.Requests;

public record DeleteReviewRequest();

public record ApproveReviewRequest();

public record RejectReviewRequest(string Reason = "مناسب نبود");

public record ReplyToReviewRequest(string Reply);

public record UpdateReviewStatusRequest(string Status);