namespace Application.Discount.Features.Queries.ValidateDiscount;

public record ValidateDiscountQuery(string Code, decimal OrderTotal, int UserId) : IRequest<ServiceResult<DiscountValidationDto>>;

public record DiscountValidationDto
{
    public bool IsValid { get; init; }
    public decimal EstimatedDiscount { get; init; }
    public string? Message { get; init; }
}