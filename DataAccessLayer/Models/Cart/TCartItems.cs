namespace DataAccessLayer.Models.Cart;

public class TCartItems : ICartItem
{
    [Key]
    public int Id { get; set; }

    public virtual TCarts? Cart { get; set; }
    public int CartId { get; set; }

    public virtual TProducts? Product { get; set; }
    public int ProductId { get; set; }

    public int Quantity { get; set; }
}
