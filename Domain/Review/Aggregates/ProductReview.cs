using Domain.Review.Events;
using Domain.Review.ValueObjects;

namespace Domain.Review.Aggregates;

public class ProductReview : AggregateRoot<ProductReviewId>, IAuditable, ISoftDeletable
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

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

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
        Guard.Against.NegativeOrZero(productId, nameof(productId));
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        if (rating < 1 || rating > 5)
            throw new DomainException("امتیاز باید بین ۱ تا ۵ باشد.");

        if (title != null && title.Trim().Length > 100)
            throw new DomainException("عنوان نظر نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");

        if (comment != null && comment.Trim().Length > 1000)
            throw new DomainException("متن نظر نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");

        var id = ProductReviewId.NewId();

        var review = new ProductReview
        {
            Id = id,
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

        review.RaiseDomainEvent(new ReviewSubmittedEvent(id, productId, userId, rating));
        return review;
    }

    public void Approve()
    {
        EnsureNotDeleted();
        if (Status == ReviewStatus.Approved) return;

        Status = ReviewStatus.Approved;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewApprovedEvent(Id, ProductId, Rating));
    }

    public void Reject(string? reason = null)
    {
        EnsureNotDeleted();
        if (Status == ReviewStatus.Rejected) return;

        if (!string.IsNullOrWhiteSpace(reason) && reason.Length > 500)
            throw new DomainException("دلیل رد نظر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

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

        if (Status == ReviewStatus.Pending)
            Approve();
    }

    public void Delete(int? deletedBy)
    {
        if (IsDeleted) return;

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewDeletedEvent(Id, ProductId, deletedBy));
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("نظر حذف شده است.");
    }

    public static class ReviewStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
    }
}