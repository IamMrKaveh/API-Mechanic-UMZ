namespace DataAccessLayer.Models.DTO;

public class AddToCartDto
{
    [Required(ErrorMessage = "شناسه نوع محصول الزامی است")]
    [Range(1, int.MaxValue, ErrorMessage = "شناسه نوع محصول باید بزرگتر از 0 باشد")]
    public int VariantId { get; set; }

    [Required(ErrorMessage = "تعداد الزامی است")]
    [Range(1, 1000, ErrorMessage = "تعداد باید حداقل 1 و حداکثر 1000 باشد")]
    public int Quantity { get; set; } = 1;

    public byte[]? RowVersion { get; set; }
}

public class UpdateCartItemDto
{
    [Required(ErrorMessage = "تعداد الزامی است")]
    [Range(0, 1000, ErrorMessage = "تعداد باید حداقل 0 و حداکثر 1000 باشد")]
    public int Quantity { get; set; }

    public byte[]? RowVersion { get; set; }
}

public class CartDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public List<CartItemDto> CartItems { get; set; } = new();
    public int TotalItems { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CartItemDto
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public string? ProductIcon { get; set; }
    public decimal TotalPrice { get; set; }
    public byte[]? RowVersion { get; set; }
    public Dictionary<string, AttributeValueDto> Attributes { get; set; } = new();
}