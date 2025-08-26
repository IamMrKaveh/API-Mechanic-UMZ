namespace DataAccessLayer.Models.Security;

public class TRateLimits
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime LastAttempt { get; set; }
}
