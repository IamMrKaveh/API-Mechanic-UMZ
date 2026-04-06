namespace Presentation.Inventory.Requests;

public record BatchAvailabilityRequest(List<int> VariantIds);