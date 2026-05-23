namespace Presentation.Inventory.Requests;

public record CreateWarehouseRequest(
    string Code,
    string Name,
    string City,
    string? Address,
    string? Phone,
    int Priority,
    bool IsDefault = false);

public record UpdateWarehouseRequest(
    string Name,
    string City,
    string? Address,
    string? Phone,
    int Priority);

public record ToggleWarehouseStatusRequest(bool IsActive);