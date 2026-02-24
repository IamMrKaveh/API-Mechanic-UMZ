namespace Application.Category.Features.Commands.CreateCategory;

public record CreateCategoryCommand : IRequest<ServiceResult<int>>
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FileDto? IconFile { get; init; }
}