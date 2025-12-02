namespace Application.DTOs;

public class PaymentInitiationDto
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
}

public class PaymentRequestResultDto
{
    public bool IsSuccess { get; set; }
    public string? RedirectUrl { get; set; }
    public string? Authority { get; set; }
    public string? Message { get; set; }
    public string PaymentUrl { get; set; }
}

public class GatewayVerificationResultDto
{
    public bool IsVerified { get; set; }
    public long? RefId { get; set; }
    public string? CardPan { get; set; }
    public string? CardHash { get; set; }
    public decimal Fee { get; set; }
    public string? Message { get; set; }
}