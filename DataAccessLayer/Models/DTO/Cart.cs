namespace DataAccessLayer.Models.DTO;

public class AddToCartDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    public int Quantity { get; set; }
}

public class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<CartItemDto> CartItems { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPrice { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int TotalPrice { get; set; }
}
