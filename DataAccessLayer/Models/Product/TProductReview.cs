namespace DataAccessLayer.Models.Product;

[Index(nameof(ProductId), nameof(CreatedAt))]
[Index(nameof(UserId))]
[Index(nameof(Status))]
public class TProductReview : IAuditable
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public virtual TProducts Product { get; set; } = null!;

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    public int? OrderId { get; set; }
    public virtual TOrders? Order { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public bool IsVerifiedPurchase { get; set; } = false;

    public int LikeCount { get; set; } = 0;
    public int DislikeCount { get; set; } = 0;

    [MaxLength(500)]
    public string? AdminReply { get; set; }

    public DateTime? RepliedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}