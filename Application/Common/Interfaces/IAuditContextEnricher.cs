namespace Application.Common.Interfaces;

public interface IAuditContextEnricher
{
    void Set(string key, string? value);

    string? Get(string key);

    IReadOnlyDictionary<string, string> Snapshot();

    void Clear();
}
