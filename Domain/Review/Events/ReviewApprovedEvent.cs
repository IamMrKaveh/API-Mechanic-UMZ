namespace Domain.Review.Events;

public sealed class ReviewApprovedEvent : Domain.Common.Events.DomainEvent
{
    public int ReviewId { get; }
    public int ProductId { get; }
    public int Rating { get; }

    public ReviewApprovedEvent(int reviewId, int productId, int rating)
    {
        ReviewId = reviewId;
        ProductId = productId;
        Rating = rating;
    }
}