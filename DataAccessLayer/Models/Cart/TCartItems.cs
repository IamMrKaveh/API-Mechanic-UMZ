namespace DataAccessLayer.Models.Cart;

[Index(nameof(CartId), nameof(VariantId), IsUnique = true)]
public class TCartItems
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "تعداد الزامی است")]
    [Range(1, 1000, ErrorMessage = "تعداد باید بین 1 تا 1000 باشد")]
    public int Quantity { get; set; }

    [ForeignKey(nameof(CartId))]
    public virtual TCarts Cart { get; set; } = null!;

    [Required(ErrorMessage = "شناسه سبد خرید الزامی است")]
    public int CartId { get; set; }

    [ForeignKey(nameof(VariantId))]
    public virtual TProductVariant Variant { get; set; } = null!;

    [Required(ErrorMessage = "شناسه محصول الزامی است")]
    public int VariantId { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}