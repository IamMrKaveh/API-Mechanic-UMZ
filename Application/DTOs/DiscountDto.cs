namespace Application.DTOs;

public class DiscountApplyResultDto
{
    public decimal DiscountAmount { get; set; }
    public int DiscountCodeId { get; set; }
}

public record ApplyDiscountDto(string Code, decimal OrderTotal);