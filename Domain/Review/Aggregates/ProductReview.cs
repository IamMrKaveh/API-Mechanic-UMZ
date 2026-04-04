using Domain.Review.Events;
using Domain.Review.ValueObjects;

namespace Domain.Review.Aggregates;

public class ProductReview : AggregateRoot<ProductReviewId>, IAuditable
{
    public int ProductId { get; private set; }
    public int UserId { get; private set; }
    public int? OrderId { get; private set; }

    public Rating Rating { get; private set; } = default!;
    public string? Title { get; private set; }
    public string? Comment { get; private set; }

    public ReviewStatus Status { get; private set; } = default!;
    public bool IsVerifiedPurchase { get; private set; }

    public int LikeCount { get; private set; }
    public int DislikeCount { get; private set; }

    public string? AdminReply { get; private set; }
    public DateTime? RepliedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public bool IsDeleted { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ProductReview()
    { }

    public static ProductReview Create(
        int productId,
        int userId,
        Rating rating,
        string? title,
        string? comment,
        bool isVerifiedPurchase,
        int? orderId = null)
    {
        Guard.Against.NegativeOrZero(productId, nameof(productId));
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.Null(rating, nameof(rating));

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

        review.RaiseDomainEvent(new ReviewSubmittedEvent(id, productId, userId, rating.Value));
        return review;
    }

    public void Approve()
    {
        if (Status == ReviewStatus.Approved) return;

        Status = ReviewStatus.Approved;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewApprovedEvent(Id, ProductId, Rating.Value));
    }

    public void Reject(string? reason = null)
    {
        if (Status == ReviewStatus.Rejected) return;

        if (!string.IsNullOrWhiteSpace(reason) && reason.Length > 500)
            throw new DomainException("دلیل رد نظر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

        Status = ReviewStatus.Rejected;
        RejectionReason = reason?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAdminReply(string reply)
    {
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

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}