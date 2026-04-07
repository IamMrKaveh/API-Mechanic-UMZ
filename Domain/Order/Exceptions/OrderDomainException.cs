namespace Domain.Order.Exceptions;

public sealed class OrderNotFoundException(Guid orderId) : Exception($"Order with ID '{orderId}' was not found.")
{
}

public sealed class InvalidOrderTransitionException(string fromStatus, string toStatus) : DomainException($"Cannot transition order from '{fromStatus}' to '{toStatus}'.")
{
}

public sealed class EmptyOrderException() : DomainException("An order must contain at least one item.")
{
}

public sealed class OrderAlreadyCancelledException(Guid orderId) : DomainException($"Order '{orderId}' has already been cancelled.")
{
}

public sealed class OrderAlreadyPaidException(Guid orderId) : DomainException($"Order '{orderId}' has already been paid.")
{
}

public sealed class OrderCancellationNotAllowedException(string currentStatus) : DomainException($"Order in status '{currentStatus}' cannot be cancelled.")
{
}