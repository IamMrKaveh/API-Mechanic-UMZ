using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Entities;
using Domain.Review.Enums;
using Domain.Review.Events;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Aggregates;

public class ProductReview : AggregateRoot<ReviewId>, IAuditable
{
    public Product.Aggregates.Product Product { get; private set; } = default!;
    public ProductId ProductId { get; private set; } = default!;
    public User.Aggregates.User User { get; private set; } = default!;
    public UserId UserId { get; private set; } = default!;
    public Order.Aggregates.Order? Order { get; private set; }
    public OrderId? OrderId { get; private set; }

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

    private readonly List<ReviewVote> _votes = new();
    public IReadOnlyCollection<ReviewVote> Votes => _votes.AsReadOnly();

    private ProductReview()
    { }

    public static ProductReview Create(
        ProductId productId,
        UserId userId,
        Rating rating,
        string? title,
        string? comment,
        bool isVerifiedPurchase,
        OrderId? orderId = null)
    {
        Guard.Against.Null(productId, nameof(productId));
        Guard.Against.Null(userId, nameof(userId));
        Guard.Against.Null(rating, nameof(rating));

        if (title != null && title.Trim().Length > 100)
            throw new DomainException("عنوان نظر نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");

        if (comment != null && comment.Trim().Length > 1000)
            throw new DomainException("متن نظر نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");

        var id = ReviewId.NewId();

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

    public void UpdateContent(Rating rating, string? title, string? comment)
    {
        Guard.Against.Null(rating, nameof(rating));

        if (Status == ReviewStatus.Approved)
            throw new DomainException("نظر تایید‌شده قابل ویرایش نیست.");

        if (title != null && title.Trim().Length > 100)
            throw new DomainException("عنوان نظر نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.");

        if (comment != null && comment.Trim().Length > 1000)
            throw new DomainException("متن نظر نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");

        Rating = rating;
        Title = title?.Trim();
        Comment = comment?.Trim();
        Status = ReviewStatus.Pending;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewContentUpdatedEvent(Id, ProductId, rating.Value));
    }

    public void Approve()
    {
        if (Status == ReviewStatus.Approved) return;

        Status = ReviewStatus.Approved;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewApprovedEvent(Id, ProductId, Rating));
    }

    public void Reject(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("دلیل رد نظر الزامی است.");

        var trimmed = reason.Trim();
        if (trimmed.Length > 500)
            throw new DomainException("دلیل رد نظر نمی‌تواند بیش از ۵۰۰ کاراکتر باشد.");

        if (Status == ReviewStatus.Rejected && RejectionReason == trimmed)
            return;

        Status = ReviewStatus.Rejected;
        RejectionReason = trimmed;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewRejectedEvent(Id, ProductId, trimmed));
    }

    public void AddAdminReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            throw new DomainException("متن پاسخ الزامی است.");

        var trimmed = reply.Trim();
        if (trimmed.Length > 1000)
            throw new DomainException("متن پاسخ نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");

        AdminReply = trimmed;
        RepliedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewAdminRepliedEvent(Id, ProductId, trimmed));

        if (Status == ReviewStatus.Pending)
            Approve();
    }

    public void UpdateAdminReply(string reply)
    {
        if (AdminReply is null)
            throw new DomainException("پاسخی برای ویرایش وجود ندارد.");

        if (string.IsNullOrWhiteSpace(reply))
            throw new DomainException("متن پاسخ الزامی است.");

        var trimmed = reply.Trim();
        if (trimmed.Length > 1000)
            throw new DomainException("متن پاسخ نمی‌تواند بیش از ۱۰۰۰ کاراکتر باشد.");

        if (AdminReply == trimmed) return;

        AdminReply = trimmed;
        RepliedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewAdminRepliedEvent(Id, ProductId, trimmed));
    }

    public void RemoveAdminReply()
    {
        if (AdminReply is null) return;

        AdminReply = null;
        RepliedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        if (IsDeleted) return;

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewDeletedEvent(Id, ProductId, UserId));
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewRestoredEvent(Id, ProductId));
    }

    public void AddLike(UserId userId) => AddVote(userId, VoteType.Like);

    public void AddDislike(UserId userId) => AddVote(userId, VoteType.Dislike);

    public void RemoveVote(UserId userId)
    {
        Guard.Against.Null(userId, nameof(userId));

        if (IsDeleted)
            throw new DomainException("امکان رأی دادن به نظر حذف‌شده وجود ندارد.");

        var existing = _votes.FirstOrDefault(v => v.UserId == userId);
        if (existing is null) return;

        _votes.Remove(existing);
        RecalculateVoteCounts();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewVoteChangedEvent(Id, LikeCount, DislikeCount));
    }

    private void AddVote(UserId userId, VoteType type)
    {
        Guard.Against.Null(userId, nameof(userId));

        if (IsDeleted)
            throw new DomainException("امکان رأی دادن به نظر حذف‌شده وجود ندارد.");

        if (Status != ReviewStatus.Approved)
            throw new DomainException("فقط نظرات تأییدشده قابل رأی‌گیری هستند.");

        if (UserId == userId)
            throw new DomainException("امکان رأی دادن به نظر خود وجود ندارد.");

        var existing = _votes.FirstOrDefault(v => v.UserId == userId);

        if (existing is not null && existing.Type == type)
            return;

        if (existing is not null)
        {
            existing.ChangeType(type);
        }
        else
        {
            _votes.Add(ReviewVote.Create(Id, userId, type));
        }

        RecalculateVoteCounts();
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ReviewVoteChangedEvent(Id, LikeCount, DislikeCount));
    }

    private void RecalculateVoteCounts()
    {
        LikeCount = _votes.Count(v => v.Type == VoteType.Like);
        DislikeCount = _votes.Count(v => v.Type == VoteType.Dislike);
    }
}
