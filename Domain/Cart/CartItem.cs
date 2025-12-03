namespace Domain.Cart;

public class CartItem
{
    public int Id { get; set; }

    public int Quantity { get; set; }

    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int VariantId { get; set; }
    public Product.ProductVariant Variant { get; set; } = null!;

    public decimal SellingPrice { get; set; }

    public byte[]? RowVersion { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}