namespace Application.Payment.Features.Shared;

public class ZarinpalRequestResponseDto
{
    public ZarinpalRequestResponseDataDto? Data { get; init; }
    public object? Errors { get; init; }
}

public class ZarinpalRequestResponseDataDto
{
    public int Code { get; init; }
    public string? Message { get; init; }
    public string? Authority { get; init; }
    public string? FeeType { get; init; }
    public decimal Fee { get; init; }
}

public class ZarinpalVerificationResponseDto
{
    public ZarinpalVerificationResponseDataDto? Data { get; init; }
    public object? Errors { get; init; }
}

public class ZarinpalVerificationResponseDataDto
{
    public int Code { get; init; }
    public string? Message { get; init; }
    public string? CardHash { get; init; }
    public string? CardPan { get; init; }
    public long RefID { get; init; }
    public string? FeeType { get; init; }
    public decimal Fee { get; init; }
}

public class ZarinpalSettingsDto
{
    public bool IsSandbox { get; init; }
    public string MerchantId { get; init; } = null!;
}

public record ZarinpalRequestDto(
    string MerchantId,
    int Amount,
    string CallbackUrl,
    string Description,
    string? Email = null,
    string? Mobile = null);

public record ZarinpalRequestResponse(int Status, string? Authority);

public record ZarinpalVerifyDto(string MerchantId, int Amount, string Authority);

public record ZarinpalVerifyResponse(int Status, long? RefId);