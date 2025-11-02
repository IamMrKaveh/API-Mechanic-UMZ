namespace DataAccessLayer.Models.DTO;

public class ZarinpalRequestDto
{
    [JsonPropertyName("merchant_id")]
    [Required]
    public string MerchantID { get; set; } = string.Empty;
    [JsonPropertyName("amount")]
    [Required]
    [Range(1000, int.MaxValue)]
    public decimal Amount { get; set; }
    [JsonPropertyName("description")]
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
    [JsonPropertyName("callback_url")]
    [Required]
    [Url]
    public string CallbackURL { get; set; } = string.Empty;

    [JsonPropertyName("metadata")]
    public ZarinpalMetadataDto? Metadata { get; set; }
}

public class ZarinpalMetadataDto
{
    [JsonPropertyName("mobile")]
    [Phone]
    public string? Mobile { get; set; }

    [JsonPropertyName("email")]
    [EmailAddress]
    public string? Email { get; set; }
}


public class ZarinpalRequestResponseDto
{
    [JsonPropertyName("data")]
    public ZarinpalRequestResponseDataDto? Data { get; set; }
    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ZarinpalRequestResponseDataDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
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
    [JsonPropertyName("merchant_id")]
    [Required]
    public string MerchantID { get; set; } = string.Empty;

    [JsonPropertyName("authority")]
    [Required]
    public string Authority { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    [Required]
    [Range(1000, int.MaxValue)]
    public decimal Amount { get; set; }
}

public class ZarinpalVerificationResponseDto
{
    [JsonPropertyName("data")]
    public ZarinpalVerificationResponseDataDto? Data { get; set; }

    [JsonPropertyName("errors")]
    public object? Errors { get; set; }
}

public class ZarinpalVerificationResponseDataDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

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

    [JsonPropertyName("status")]
    public int Status { get; set; }
}