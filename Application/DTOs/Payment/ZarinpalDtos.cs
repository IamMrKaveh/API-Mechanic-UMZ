namespace Application.DTOs.Payment;

public class ZarinpalRequestDto
{
    [JsonPropertyName("merchant_id")] public string? MerchantID { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("description")]
    public required string Description { get; set; }

    [JsonPropertyName("callback_url")]
    public required string CallbackURL { get; set; }

    [JsonPropertyName("metadata")]
    public ZarinpalMetadataDto? Metadata { get; set; }
}

public class ZarinpalMetadataDto
{
    [JsonPropertyName("mobile")] public string? Mobile { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class ZarinpalRequestResponseDto
{
    [JsonPropertyName("data")] public ZarinpalRequestResponseDataDto? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ZarinpalRequestResponseDataDto
{
    [JsonPropertyName("code")] public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("authority")]
    public string? Authority { get; set; }

    [JsonPropertyName("fee_type")]
    public string? FeeType { get; set; }

    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }
}

public class ZarinpalVerificationRequestDto
{
    [JsonPropertyName("merchant_id")] public string? MerchantID { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("authority")]
    public required string Authority { get; set; }
}

public class ZarinpalVerificationResponseDto
{
    [JsonPropertyName("data")] public ZarinpalVerificationResponseDataDto? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ZarinpalVerificationResponseDataDto
{
    [JsonPropertyName("code")] public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("card_hash")]
    public string? CardHash { get; set; }

    [JsonPropertyName("card_pan")]
    public string? CardPan { get; set; }

    [JsonPropertyName("ref_id")]
    public long RefID { get; set; }

    [JsonPropertyName("fee_type")]
    public string? FeeType { get; set; }

    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }
}

public class ZarinpalSettingsDto
{
    public bool IsSandbox { get; init; }
    public required string MerchantId { get; init; }
}