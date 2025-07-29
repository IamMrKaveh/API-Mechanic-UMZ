namespace DataAccessLayer.Models.User.Interfaces;

public interface IUser : IBaseEntity
{
    public string? PhoneNumber { get; set; }

    public ICollection<TOrders>? UserOrders
    { get; set; }
}