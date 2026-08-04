namespace Application.Common.Services;

public sealed class AuditContextEnricher : IAuditContextEnricher
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return;

        if (value is null)
        {
            _values.Remove(key);
            return;
        }

        _values[key] = value;
    }

    public string? Get(string key) => _values.TryGetValue(key, out var v) ? v : null;

    public IReadOnlyDictionary<string, string> Snapshot() => _values;

    public void Clear() => _values.Clear();
}
