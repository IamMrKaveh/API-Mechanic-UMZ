namespace Application.DTOs.Order;

public class CreateOrderDto { public int UserId { get; set; } public required string ReceiverName { get; set; } public int UserAddressId { get; set; } public int ShippingMethodId { get; set; } public int OrderStatusId { get; set; } public string? DiscountCode { get; set; } public List<CreateOrderItemDto> OrderItems { get; set; } = []; }

public class UpdateOrderDto { public int? OrderStatusId { get; set; } public int? ShippingMethodId { get; set; } public int? UserAddressId { get; set; } public DateTime? DeliveryDate { get; set; } public required string RowVersion { get; set; } }

public class CreateOrderFromCartDto { public int? UserAddressId { get; set; } public CreateUserAddressDto? NewAddress { get; set; } public bool SaveNewAddress { get; set; } public int ShippingMethodId { get; set; } public string? DiscountCode { get; set; } public List<CheckoutItemPriceDto> ExpectedItems { get; set; } = []; public string? CallbackUrl { get; set; } }

public class CreateOrderItemDto { public int OrderId { get; set; } public int VariantId { get; set; } public int Quantity { get; set; } public decimal SellingPrice { get; set; } }

public class UpdateOrderItemDto { public int? Quantity { get; set; } public decimal? SellingPrice { get; set; } public required string RowVersion { get; set; } }

public class CheckoutItemPriceDto { public int VariantId { get; set; } public decimal Price { get; set; } public string? RowVersion { get; set; } }

public class CreateOrderStatusDto { public required string Name { get; set; } public string? Icon { get; set; } }

public class UpdateOrderStatusDto { public string? Name { get; set; } public string? Icon { get; set; } public string? RowVersion { get; set; } }

public class UpdateOrderStatusByIdDto { public int OrderStatusId { get; set; } public required string RowVersion { get; set; } }

public class OrderStatusDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public string? Icon { get; set; } public bool IsDeleted { get; set; } public bool IsActive { get; set; } }

public class OrderDto { public int Id { get; set; } public int UserId { get; set; } public string? ReceiverName { get; set; } public UserAddressDto? UserAddress { get; set; } public decimal TotalAmount { get; set; } public decimal TotalProfit { get; set; } public decimal ShippingCost { get; set; } public decimal DiscountAmount { get; set; } public decimal FinalAmount { get; set; } public DateTime CreatedAt { get; set; } public DateTime? PaymentDate { get; set; } public int OrderStatusId { get; set; } public OrderStatusDto? OrderStatus { get; set; } public int ShippingMethodId { get; set; } public ShippingMethodDto? ShippingMethod { get; set; } public bool IsPaid { get; set; } public string? IdempotencyKey { get; set; } public ICollection<OrderItemDto> OrderItems { get; set; } = []; }

public class OrderItemDto { public int Id { get; set; } public int VariantId { get; set; } public string? ProductName { get; set; } public decimal PurchasePrice { get; set; } public decimal SellingPrice { get; set; } public int Quantity { get; set; } public decimal Amount { get; set; } public decimal Profit { get; set; } }