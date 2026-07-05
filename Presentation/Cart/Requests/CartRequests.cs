namespace Presentation.Cart.Requests;

public record AddCartItemRequest(Guid VariantId, int Quantity);

public record UpdateCartItemQuantityRequest(int Quantity);