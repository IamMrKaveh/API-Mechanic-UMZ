namespace DataAccessLayer.Models.Cart;

public class TCartItems
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CartId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }

    public virtual TCarts? Cart { get; set; }
    public virtual TProducts? Product { get; set; }
}