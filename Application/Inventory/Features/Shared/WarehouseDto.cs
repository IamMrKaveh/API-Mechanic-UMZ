namespace Application.Inventory.Features.Shared;

public record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string City,
    string? Address,
    string? Phone,
    int Priority,
    bool IsActive,
    bool IsDefault,
    DateTime CreatedAt
);