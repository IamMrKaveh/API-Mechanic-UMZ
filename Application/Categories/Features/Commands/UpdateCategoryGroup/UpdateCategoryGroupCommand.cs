namespace Application.Categories.Features.Commands.UpdateCategoryGroup;

public record UpdateCategoryGroupCommand : IRequest<ServiceResult>
{
    public int CategoryId { get; init; }
    public int GroupId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public FileDto? IconFile { get; init; }
    public required string RowVersion { get; init; }
}