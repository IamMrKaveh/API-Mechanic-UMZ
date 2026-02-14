namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(string Code, decimal OrderTotal, int UserId) : IRequest<ServiceResult<DiscountValidationDto>>;

public class DiscountValidationDto
{
    public bool IsValid { get; set; }
    public decimal EstimatedDiscount { get; set; }
    public string? Message { get; set; }
}