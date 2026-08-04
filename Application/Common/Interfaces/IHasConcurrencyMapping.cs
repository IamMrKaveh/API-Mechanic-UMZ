namespace Application.Common.Interfaces;

public interface IHasConcurrencyMapping
{
    string ConcurrencyMessage => "این منبع همزمان توسط کاربر دیگری تغییر داده شد. لطفاً دوباره تلاش کنید.";
}
