namespace Domain.Audit.ValueObjects;

public sealed record AuditEventType
{
    public static readonly AuditEventType Authentication = new("Authentication");
    public static readonly AuditEventType Security = new("SecurityEvent");
    public static readonly AuditEventType Order = new("OrderEvent");
    public static readonly AuditEventType Payment = new("PaymentEvent");
    public static readonly AuditEventType Inventory = new("InventoryEvent");
    public static readonly AuditEventType Product = new("ProductEvent");
    public static readonly AuditEventType AdminAction = new("AdminEvent");
    public static readonly AuditEventType System = new("SystemEvent");
    public static readonly AuditEventType Error = new("Error");
    public static readonly AuditEventType Warning = new("Warning");
    public static readonly AuditEventType Information = new("Information");
    public static readonly AuditEventType Debug = new("Debug");

    public string Value { get; }

    private AuditEventType(string value) => Value = value;

    public static AuditEventType From(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new DomainException("AuditEventType cannot be empty.")
        : new(value.Trim());

    public override string ToString() => Value;

    public static implicit operator string(AuditEventType eventType) => eventType.Value;
}