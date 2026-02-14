namespace Application.Categories.Features.Commands.CreateCategoryGroup;

public record CreateCategoryGroupCommand : IRequest<ServiceResult<int>>
{
    public int CategoryId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FileDto? IconFile { get; init; }
}