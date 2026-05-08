using Domain.Audit.ValueObjects;

namespace Infrastructure.Audit.Converters;

internal sealed class AuditLogIdConverter : StronglyTypedIdConverter<AuditLogId>
{
    public AuditLogIdConverter() : base(AuditLogId.From)
    {
    }
}