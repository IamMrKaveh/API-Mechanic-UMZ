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
    [JsonPropertyName("mobile")]
    [Phone]
    public string? Mobile { get; set; }
    [JsonPropertyName("email")]
    [EmailAddress]
    public string? Email { get; set; }
}

public class ZarinpalRequestResponseDto
{
    public int Status { get; set; }
    public string? Authority { get; set; }
    public List<object> Errors { get; set; } = new();
}

public class ZarinpalVerificationRequestDto
{
    [Required]
    public string MerchantID { get; set; } = string.Empty;
    [Required]
    public string Authority { get; set; } = string.Empty;
    [Required]
    [Range(1000, int.MaxValue)]
    public decimal Amount { get; set; }
}

public class ZarinpalVerificationResponseDto
{
    public int Status { get; set; }
    public long RefID { get; set; }
    public List<object> Errors { get; set; } = new();
    public string? CardHash { get; set; }
    public string? CardPan { get; set; }
    public long? Fee { get; set; }
    public string? FeeType { get; set; }
}