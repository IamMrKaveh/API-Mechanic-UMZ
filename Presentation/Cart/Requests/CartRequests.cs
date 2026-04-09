namespace Presentation.Cart.Requests;

public record AddToCartRequest(Guid VariantId, int Quantity);

public record UpdateCartItemRequest(int Quantity);