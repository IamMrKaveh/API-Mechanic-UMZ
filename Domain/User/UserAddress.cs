namespace Domain.User;

public class UserAddress : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public required string ReceiverName { get; set; }

    public required string PhoneNumber { get; set; }

    public required string Province { get; set; }

    public required string City { get; set; }

    public required string Address { get; set; }

    public required string PostalCode { get; set; }

    public bool IsDefault { get; set; }

    public new bool IsActive { get; set; } = true;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }
}