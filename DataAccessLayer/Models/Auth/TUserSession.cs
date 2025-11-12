namespace DataAccessLayer.Models.Auth;

[Index(nameof(SessionToken), IsUnique = true)]
[Index(nameof(UserId))]
[Index(nameof(ExpiresAt))]
public class TUserSession
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }
    public virtual TUsers User { get; set; } = null!;

    [Required, MaxLength(512)]
    public string SessionToken { get; set; } = string.Empty;

    [MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }

    public bool IsActive { get; set; } = true;
}