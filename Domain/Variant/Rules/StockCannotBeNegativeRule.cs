namespace Domain.Variant.Rules;

public sealed class StockCannotBeNegativeRule(int currentStock, int requestedDeduction, bool isUnlimited) : IBusinessRule
{
    private readonly int _currentStock = currentStock;
    private readonly int _requestedDeduction = requestedDeduction;
    private readonly bool _isUnlimited = isUnlimited;

    public bool IsBroken()
    {
        if (_isUnlimited)
            return false;

        return _currentStock - _requestedDeduction < 0;
    }

    public string Message => $"موجودی نمی‌تواند منفی شود. موجودی فعلی: {_currentStock}، کاهش درخواستی: {_requestedDeduction}";
}