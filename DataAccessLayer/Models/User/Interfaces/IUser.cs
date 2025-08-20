namespace DataAccessLayer.Models.User.Interfaces;

public interface IUser
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; }

    public ICollection<TOrders>? UserOrders
    { get; set; }
}