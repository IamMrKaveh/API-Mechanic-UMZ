namespace Application.DTOs;

public class ZarinpalSettingsDto
{
    public bool IsSandbox { get; init; }
    public string MerchantId { get; init; }
}

public class FrontendUrlsDto
{
    public string BaseUrl { get; init; }
}