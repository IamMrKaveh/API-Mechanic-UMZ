namespace Application.Order.Features.Shared;

public sealed class OrderStatusDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public bool AllowCancel { get; init; }
    public bool AllowEdit { get; init; }
    public string? RowVersion { get; init; }
}
