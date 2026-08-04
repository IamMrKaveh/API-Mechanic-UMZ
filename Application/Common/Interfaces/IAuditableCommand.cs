namespace Application.Common.Interfaces;

public interface IAuditableCommand
{
    string AuditEventType { get; }

    string AuditAction { get; }

    string? AuditEntityType => null;

    string? AuditEntityId => null;

    string? BuildAuditDetails() => null;

    string? BuildAuditDetails(IAuditContextEnricher enricher)
    {
        var baseDetails = BuildAuditDetails();
        var snapshot = enricher.Snapshot();

        if (snapshot.Count == 0)
            return baseDetails;

        var enriched = string.Join(
            "; ",
            snapshot.Select(kv => $"{kv.Key}={kv.Value}"));

        return string.IsNullOrWhiteSpace(baseDetails)
            ? enriched
            : $"{baseDetails} | {enriched}";
    }
}
