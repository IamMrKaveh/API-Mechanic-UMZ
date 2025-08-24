namespace DataAccessLayer.Models.Cart;

public class TCarts
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public virtual TUsers? User { get; set; }


    public int TotalItems { get; set; }

    public int TotalPrice { get; set; }

    public virtual ICollection<TCartItems> CartItems { get; set; } = new List<TCartItems>();
}