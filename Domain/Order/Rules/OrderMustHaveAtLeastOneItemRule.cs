namespace Domain.Order.Rules;

public class OrderMustHaveAtLeastOneItemRule : IBusinessRule
{
    private readonly IReadOnlyCollection<OrderItem> _items;

    public OrderMustHaveAtLeastOneItemRule(IReadOnlyCollection<OrderItem> items)
    {
        _items = items;
    }

    public bool IsBroken() => !_items.Any() || _items.All(i => i.Quantity <= 0);

    public string Message => "سفارش باید حداقل یک آیتم داشته باشد.";
}