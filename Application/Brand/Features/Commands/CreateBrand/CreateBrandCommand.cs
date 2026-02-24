namespace Application.Brand.Features.Commands.CreateBrand;

public record CreateBrandCommand : IRequest<ServiceResult<int>>
{
    public int CategoryId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FileDto? IconFile { get; init; }
}