namespace DataAccessLayer.Models.Security;

public class TRateLimit
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(200)]
    public string Key { get; set; } = string.Empty;
    [Required]
    public int Count { get; set; }
    [Required]
    public DateTime LastAttempt { get; set; }
}