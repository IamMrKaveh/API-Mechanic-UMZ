using Domain.Order.Entities;

namespace Domain.Order.Rules;

public class OrderMustHaveAtLeastOneItemRule(IReadOnlyCollection<OrderItem> items) : IBusinessRule
{
    private readonly IReadOnlyCollection<OrderItem> _items = items;

    public bool IsBroken() => !_items.Any() || _items.All(i => i.Quantity <= 0);

    public string Message => "سفارش باید حداقل یک آیتم داشته باشد.";
}