namespace Application.Cache.Features.Shared;

/// <summary>
/// Read Model برای Cache موجودی واریانت
/// </summary>
public class VariantAvailabilityCacheDto
{
    public int VariantId { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public bool IsInStock { get; set; }
    public bool IsUnlimited { get; set; }
    public DateTime LastUpdated { get; set; }
}