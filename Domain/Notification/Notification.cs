namespace Domain.Notification;

public class Notification : IAuditable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User.User User { get; set; } = null!;

    public required string Title { get; set; }

    public required string Message { get; set; }

    public required string Type { get; set; }

    public bool IsRead { get; set; }

    public string? ActionUrl { get; set; }

    public int? RelatedEntityId { get; set; }

    public string? RelatedEntityType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}