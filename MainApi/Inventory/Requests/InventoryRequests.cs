namespace MainApi.Inventory.Requests;

public record BatchAvailabilityRequest(List<int> VariantIds);