namespace DataAccessLayer.Models.User;

public interface IUserOtp
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string OtpHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }
}
