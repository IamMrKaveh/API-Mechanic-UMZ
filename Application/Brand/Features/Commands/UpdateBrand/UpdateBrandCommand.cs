namespace Application.Brand.Features.Commands.UpdateBrand;

public record UpdateBrandCommand : IRequest<ServiceResult>
{
    public int CategoryId { get; init; }
    public int BrandId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FileDto? IconFile { get; init; }
    public required string RowVersion { get; init; }
}