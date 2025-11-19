namespace Domain.Product;

public class ProductReview : IAuditable, ISoftDeletable, IActivatable
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public int? OrderId { get; set; }
    public Order.Order? Order { get; set; }

    public int Rating { get; set; }

    public string? Title { get; set; }

    public string? Comment { get; set; }

    public string Status { get; set; } = "Pending";

    public bool IsVerifiedPurchase { get; set; }

    public int LikeCount { get; set; }

    public int DislikeCount { get; set; }

    public string? AdminReply { get; set; }

    public DateTime? RepliedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;
}