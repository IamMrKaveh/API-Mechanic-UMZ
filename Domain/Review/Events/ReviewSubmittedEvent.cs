namespace Domain.Review.Events;

public sealed class ReviewSubmittedEvent : DomainEvent
{
    public int ReviewId { get; }
    public int ProductId { get; }
    public int UserId { get; }

    public ReviewSubmittedEvent(int reviewId, int productId, int userId)
    {
        ReviewId = reviewId;
        ProductId = productId;
        UserId = userId;
    }
}