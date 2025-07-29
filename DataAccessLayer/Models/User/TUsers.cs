namespace DataAccessLayer.Models.User;

public class TUsers : IUser
{
    public int Id { get; set; }

    public string? Name { get; set; }
    public string? Icon { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual ICollection<TOrders>? UserOrders 
    { get; set; }
}