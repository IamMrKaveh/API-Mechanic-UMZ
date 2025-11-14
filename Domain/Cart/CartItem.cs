namespace Domain.Cart;

public class CartItem
{
    public int Id { get; set; }

    public int Quantity { get; set; }

    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public int VariantId { get; set; }
    public Product.ProductVariant Variant { get; set; } = null!;

    public byte[]? RowVersion { get; set; }
}