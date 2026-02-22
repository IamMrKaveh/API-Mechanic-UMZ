namespace Infrastructure.Payment.ZarinPal;

internal class ZarinpalRequestResponseDto
{
    [JsonPropertyName("data")] public ZarinpalRequestResponseDataDto? Data { get; set; }
    [JsonPropertyName("errors")] public object? Errors { get; set; }
}

internal class ZarinpalRequestResponseDataDto
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("authority")] public string? Authority { get; set; }
    [JsonPropertyName("fee_type")] public string? FeeType { get; set; }
    [JsonPropertyName("fee")] public decimal Fee { get; set; }
}

internal class ZarinpalVerificationResponseDto
{
    [JsonPropertyName("data")] public ZarinpalVerificationResponseDataDto? Data { get; set; }
    [JsonPropertyName("errors")] public object? Errors { get; set; }
}

internal class ZarinpalVerificationResponseDataDto
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("card_hash")] public string? CardHash { get; set; }
    [JsonPropertyName("card_pan")] public string? CardPan { get; set; }
    [JsonPropertyName("ref_id")] public long RefID { get; set; }
    [JsonPropertyName("fee_type")] public string? FeeType { get; set; }
    [JsonPropertyName("fee")] public decimal Fee { get; set; }
}

public class ZarinpalSettingsDto
{
    public bool IsSandbox { get; init; }
    public string MerchantId { get; init; } = null!;
}