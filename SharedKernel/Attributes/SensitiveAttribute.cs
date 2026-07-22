namespace SharedKernel.Attributes;

[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = false,
    AllowMultiple = false)]
public sealed class SensitiveAttribute(string category = "PII") : Attribute
{
    public string Category { get; } = string.IsNullOrWhiteSpace(category) ? "PII" : category;
}
