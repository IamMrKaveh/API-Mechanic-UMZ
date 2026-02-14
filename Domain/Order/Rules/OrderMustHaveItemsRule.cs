namespace Domain.Order.Rules;

public sealed class OrderMustHaveItemsRule : IBusinessRule
{
    private readonly IReadOnlyCollection<OrderItem> _items;

    public OrderMustHaveItemsRule(IReadOnlyCollection<OrderItem> items)
    {
        _items = items;
    }

    public bool IsBroken() => !_items.Any() || _items.All(i => i.Quantity <= 0);

    public string Message => "سفارش باید حداقل یک آیتم داشته باشد.";
}