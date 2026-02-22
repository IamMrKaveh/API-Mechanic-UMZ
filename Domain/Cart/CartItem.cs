namespace Domain.Cart;

/// <summary>
/// آیتم سبد خرید - فقط از طریق Cart قابل تغییر
/// </summary>
public class CartItem : BaseEntity
{
    public int CartId { get; private set; }
    public int VariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal SellingPrice { get; private set; }
    public DateTime AddedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Cart? Cart { get; private set; }
    public ProductVariant? Variant { get; private set; }

    public decimal TotalPrice => Quantity * SellingPrice;

    private const int MinQuantity = 1;
    private const int MaxQuantity = 1000;

    private CartItem()
    { }

    #region Factory Method

    /// <summary>
    /// ایجاد آیتم جدید - فقط از طریق Cart قابل فراخوانی
    /// </summary>
    internal static CartItem Create(Cart cart, int variantId, int quantity, decimal sellingPrice)
    {
        Guard.Against.Null(cart, nameof(cart));
        Guard.Against.NegativeOrZero(variantId, nameof(variantId));

        return new CartItem
        {
            CartId = cart.Id,
            Cart = cart,
            VariantId = variantId,
            Quantity = quantity,
            SellingPrice = sellingPrice,
            AddedAt = DateTime.UtcNow
        };
    }

    #endregion Factory Method

    #region Internal Update Methods - فقط Cart می‌تواند صدا بزند

    internal void UpdateQuantity(int newQuantity)
    {
        Quantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    internal void UpdatePrice(decimal newPrice)
    {
        SellingPrice = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Internal Update Methods - فقط Cart می‌تواند صدا بزند

    #region Query Methods

    public bool HasPriceChanged(decimal currentPrice)
    {
        return SellingPrice != currentPrice;
    }

    public decimal CalculatePriceDifference(decimal currentPrice)
    {
        return currentPrice - SellingPrice;
    }

    #endregion Query Methods
}