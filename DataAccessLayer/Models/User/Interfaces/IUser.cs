namespace DataAccessLayer.Models.User.Interfaces;

public interface IUser
{
    public int Id { get; set; }

    public string PhoneNumber { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
}