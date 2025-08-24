namespace DataAccessLayer.Models.DTO;

public class AddToCartDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be greater than 0")]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
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

    [Required]
    public int ProductId { get; set; }

    [Required]
    public string ProductName { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int SellingPrice { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public int TotalPrice { get; set; }
}