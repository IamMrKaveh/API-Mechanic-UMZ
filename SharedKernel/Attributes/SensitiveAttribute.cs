namespace SharedKernel.Attributes;

[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Interface,
    Inherited = true,
    AllowMultiple = false)]
public sealed class SensitiveAttribute : Attribute
{
}
