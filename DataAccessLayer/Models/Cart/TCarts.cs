namespace DataAccessLayer.Models.Cart;

public class TCarts : ICart
{
    [Key]
    public int Id { get; set; }
    public virtual TUsers? User { get; set; }
    public int UserId { get; set; }

    public virtual ICollection<TCartItems>? CartItems 
    { get; set; }
}