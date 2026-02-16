namespace Domain.Review;

public class ProductReview : BaseEntity, IAuditable, ISoftDeletable
{
    public int ProductId { get; private set; }
    public int UserId { get; private set; }
    public int? OrderId { get; private set; }
    public int Rating { get; private set; }
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    public string Status { get; private set; } = ReviewStatus.Pending;
    public bool IsVerifiedPurchase { get; private set; }

    public int LikeCount { get; private set; }
    public int DislikeCount { get; private set; }
    public string? AdminReply { get; private set; }
    public DateTime? RepliedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    // Audit & Soft Delete
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Navigation
    public Product.Product? Product { get; private set; }

    public User.User? User { get; private set; }
    public Order.Order? Order { get; private set; }

    private ProductReview()
    { }

    public static ProductReview Create(
        int productId,
        int userId,
        int rating,
        string? title,
        string? comment,
        bool isVerifiedPurchase,
        int? orderId = null)
    {
        ValidateRating(rating);
        ValidateTitle(title);
        ValidateComment(comment);

        return new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            OrderId = orderId,
            Rating = rating,
            Title = title?.Trim(),
            Comment = comment?.Trim(),
            IsVerifiedPurchase = isVerifiedPurchase,
            CreatedAt = DateTime.UtcNow,
            Status = ReviewStatus.Pending
        };
    }

    public void Approve()
    {
        EnsureNotDeleted();

        if (Status == ReviewStatus.Approved)
            return;

        Status = ReviewStatus.Approved;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string? reason = null)
    {
        EnsureNotDeleted();

        if (Status == ReviewStatus.Rejected)
            return;

        ValidateRejectionReason(reason);

        Status = ReviewStatus.Rejected;
        RejectionReason = reason?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAdminReply(string reply)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(reply))
            throw new DomainException("متن پاسخ الزامی است.");

        if (reply.Trim().Length > 1000)
            throw new DomainException("متن پاسخ نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");

        AdminReply = reply.Trim();
        RepliedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Auto-approve if still pending
        if (Status == ReviewStatus.Pending)
            Approve();
    }

    public void IncrementLikes()
    {
        LikeCount++;
    }

    public void IncrementDislikes()
    {
        DislikeCount++;
    }

    public void Delete(int? deletedBy)
    {
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public bool IsPending => Status == ReviewStatus.Pending;
    public bool IsApproved => Status == ReviewStatus.Approved;
    public bool IsRejected => Status == ReviewStatus.Rejected;

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("نظر حذف شده است.");
    }

    private static void ValidateRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new DomainException("امتیاز باید بین ۱ تا ۵ باشد.");
    }

    private static void ValidateTitle(string? title)
    {
        if (title != null && title.Trim().Length > 100)
            throw new DomainException("عنوان نظر نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");
    }

    private static void ValidateComment(string? comment)
    {
        if (comment != null && comment.Trim().Length > 1000)
            throw new DomainException("متن نظر نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");
    }

    private static void ValidateRejectionReason(string? reason)
    {
        if (reason != null && reason.Trim().Length > 500)
            throw new DomainException("دلیل رد نظر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");
    }

    public static class ReviewStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }

    public ProductReview AddReview(
        int userId, int rating, string? title, string? comment,
        bool isVerifiedPurchase, int? orderId = null)
    {
        EnsureNotDeleted();

        if (_reviews.Any(r => !r.IsDeleted && r.UserId == userId && r.OrderId == orderId))
            throw new DomainException("شما قبلاً برای این محصول نظر ثبت کرده‌اید.");

        var review = ProductReview.Create(Id, userId, rating, title, comment, isVerifiedPurchase, orderId);
        _reviews.Add(review);
        RecalculateRating();

        return review;
    }

    public void ApproveReview(int reviewId)
    {
        EnsureNotDeleted();
        GetReviewOrThrow(reviewId).Approve();
        RecalculateRating();
    }

    public void RejectReview(int reviewId)
    {
        EnsureNotDeleted();
        GetReviewOrThrow(reviewId).Reject();
        RecalculateRating();
    }

    public void ReplyToReview(int reviewId, string reply)
    {
        EnsureNotDeleted();
        GetReviewOrThrow(reviewId).AddAdminReply(reply);
    }

    public void DeleteReview(int reviewId, int? deletedBy = null)
    {
        GetReviewOrThrow(reviewId).Delete(deletedBy);
        RecalculateRating();
    }
}