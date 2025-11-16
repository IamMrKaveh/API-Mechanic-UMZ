namespace Application.DTOs;

public class CreateReviewDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}

public class UpdateReviewStatusDto
{
    [Required]
    public string Status { get; set; }
}

public class ProductReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsVerifiedPurchase { get; set; }
    public DateTime CreatedAt { get; set; }
}