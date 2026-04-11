using Application.Common.Extensions;
using Application.Product.Features.Shared;
using Domain.Product.Aggregates;
using Mapster;

namespace Application.Product.Mapping;

public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductDetailDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.BrandId, src => src.BrandId.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsFeatured, src => src.IsFeatured)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Ignore(dest => dest.CategoryName)
            .Ignore(dest => dest.BrandName)
            .Ignore(dest => dest.RowVersion)
            .Ignore(dest => dest.PrimaryImageUrl)
            .Ignore(dest => dest.Variants)
            .IgnoreNonMapped(true);

        config.NewConfig<Product, ProductListItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.BrandId, src => src.BrandId.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsFeatured, src => src.IsFeatured)
            .Map(dest => dest.IsDeleted, src => src.IsDeleted)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Ignore(dest => dest.CategoryName)
            .Ignore(dest => dest.BrandName)
            .Ignore(dest => dest.MinPrice)
            .Ignore(dest => dest.HasStock)
            .Ignore(dest => dest.PrimaryImageUrl)
            .Ignore(dest => dest.RowVersion)
            .IgnoreNonMapped(true);
    }
}