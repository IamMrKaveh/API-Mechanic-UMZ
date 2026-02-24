namespace Application.Category.Features.Commands.UpdateCategory;

public record UpdateCategoryCommand : IRequest<ServiceResult>
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public FileDto? IconFile { get; init; }
    public required string RowVersion { get; init; }
}