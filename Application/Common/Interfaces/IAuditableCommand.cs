namespace Application.Common.Interfaces;

public interface IAuditableCommand
{
    string AuditEventType { get; }

    string AuditAction { get; }

    string? AuditEntityType => null;

    string? AuditEntityId => null;
}