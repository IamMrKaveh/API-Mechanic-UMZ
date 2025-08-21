namespace DataAccessLayer.Models.User;
public class TUsers : IUser
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsAdmin { get; set; } = false;

    public virtual ICollection<TOrders> UserOrders 
    { get; set; } = new List<TOrders>();

    public virtual ICollection<TUserOtp> UserOtps { get; set; } = new List<TUserOtp>();

}