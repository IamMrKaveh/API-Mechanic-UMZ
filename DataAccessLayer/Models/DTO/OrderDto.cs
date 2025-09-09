namespace DataAccessLayer.Models.DTO;

public class CreateOrderDto
{
    private string? _name;
    private string? _address;
    private string? _postalCode;
    public int UserId { get; set; }
    public string? Name { get => _name; set => _name = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public string? Address { get => _address; set => _address = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public string? PostalCode { get => _postalCode; set => _postalCode = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public int OrderStatusId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}

public class UpdateOrderDto
{
    private string? _name;
    private string? _address;
    private string? _postalCode;
    public string? Name { get => _name; set => _name = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public string? Address { get => _address; set => _address = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public string? PostalCode { get => _postalCode; set => _postalCode = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public int? OrderStatusId { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class CreateOrderStatusDto
{
    private string? _name;
    private string? _icon;
    public string? Name { get => _name; set => _name = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public string? Icon { get => _icon; set => _icon = value == null ? null : new HtmlSanitizer().Sanitize(value); }
}

public class UpdateOrderStatusDto
{
    private string? _name;
    private string? _icon;
    public int OrderStatusId { get; set; }
    public string? Name { get => _name; set => _name = value == null ? null : new HtmlSanitizer().Sanitize(value); }
    public string? Icon { get => _icon; set => _icon = value == null ? null : new HtmlSanitizer().Sanitize(value); }
}

public class CreateOrderItemDto
{
    public int UserOrderId { get; set; }
    public int ProductId { get; set; }
    [Range(1, int.MaxValue)]
    public int SellingPrice { get; set; }
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class UpdateOrderItemDto
{
    public int? SellingPrice { get; set; }
    public int? Quantity { get; set; }
    public byte[]? RowVersion { get; set; }
}

public class PublicOrderItemProductViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public string CategoryName { get; set; }
}

public class PublicOrderItemOrderViewDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicOrderItemViewDto
{
    public int Id { get; set; }
    public int SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int Amount { get; set; }
    public PublicOrderItemProductViewDto Product { get; set; }
    public PublicOrderItemOrderViewDto Order { get; set; }
}