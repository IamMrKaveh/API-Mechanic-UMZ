namespace DataAccessLayer.Models.DTO;

public class AddToCartDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be greater than 0")]
    public int ProductId { get; set; }
    [Required]
    [Range(1, 1000, ErrorMessage = "Quantity must be at least 1 and at most 1000")]
    public int Quantity { get; set; } = 1;
    public string? Color { get; set; }
    public string? Size { get; set; }
}

public class UpdateCartItemDto
{
    [Required]
    [Range(0, 1000, ErrorMessage = "Quantity must be at least 0 and at most 1000")]
    public int Quantity { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class CartDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public List<CartItemDto> CartItems { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    [Required]
    public int ProductId { get; set; }
    [Required]
    public string ProductName { get; set; } = string.Empty;
    [Range(0, int.MaxValue)]
    public decimal SellingPrice { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    public string? Color { get; set; }
    public string? Size { get; set; }

    public string? ProductIcon { get; set; }
    public decimal TotalPrice { get; set; }
    public byte[]? RowVersion { get; set; }
}