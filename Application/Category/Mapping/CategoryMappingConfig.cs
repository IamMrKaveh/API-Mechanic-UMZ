using Application.Category.Features.Commands.CreateCategory;
using Application.Category.Features.Commands.UpdateCategory;
using Application.Category.Features.Shared;
using Domain.Category.Aggregates;
using Mapster;

namespace Application.Category.Mapping;

public class CategoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);

        config.NewConfig<CreateCategoryDto, CreateCategoryCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.SortOrder, src => src.SortOrder);

        config.NewConfig<UpdateCategoryDto, UpdateCategoryCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.Slug, src => src.Slug)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.RowVersion, src => src.RowVersion)
            .IgnoreNonMapped(true);
    }
}