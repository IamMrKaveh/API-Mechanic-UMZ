namespace Application.Common.Mapping;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<CategoryGroupCreateDto, Domain.Category.CategoryGroup>()
             .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        CreateMap<CategoryGroupUpdateDto, Domain.Category.CategoryGroup>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        CreateMap<Domain.Category.CategoryGroup, CategoryGroupViewDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<CategoryCreateDto, Domain.Category.Category>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        CreateMap<CategoryUpdateDto, Domain.Category.Category>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()));

        CreateMap<Domain.Category.Category, CategoryViewDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore());

        CreateMap<Domain.Category.Category, CategoryDetailViewDto>()
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore())
            .ForMember(dest => dest.CategoryGroups, opt => opt.Ignore());

        CreateMap<Domain.Category.CategoryGroup, CategoryGroupSummaryDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count))
            .ForMember(dest => dest.InStockProducts, opt => opt.MapFrom(src => src.Products.Count(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0))))
            .ForMember(dest => dest.TotalValue, opt => opt.MapFrom(src => (long)src.Products.SelectMany(p => p.Variants).Sum(v => v.PurchasePrice * v.Stock)))
            .ForMember(dest => dest.TotalSellingValue, opt => opt.MapFrom(src => (long)src.Products.SelectMany(p => p.Variants).Sum(v => v.SellingPrice * v.Stock)))
            .ForMember(dest => dest.IconUrl, opt => opt.Ignore());

        CreateMap<Domain.Product.Product, ProductSummaryDto>()
            .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.TotalStock))
            .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src => src.MinPrice))
            .ForMember(dest => dest.PurchasePrice, opt => opt.MapFrom(src => src.MaxPrice))
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.TotalStock > 0 || src.Variants.Any(v => v.IsUnlimited)))
            .ForMember(dest => dest.Icon, opt => opt.Ignore());

        CreateMap<ProductDto, Domain.Product.Product>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description != null ? src.Description.Trim() : null));

        CreateMap<CreateProductVariantDto, Domain.Product.ProductVariant>()
            .ForMember(dest => dest.VariantAttributes, opt => opt.MapFrom(src => src.AttributeValueIds.Select(id => new Domain.Product.Attribute.ProductVariantAttribute { AttributeValueId = id })));

        CreateMap<Domain.Product.Product, PublicProductViewDto>()
            .ForMember(dest => dest.CategoryGroup, opt => opt.MapFrom(src => src.CategoryGroup != null ? new { src.CategoryGroup.Id, src.CategoryGroup.Name, CategoryName = src.CategoryGroup.Category.Name } : null))
            .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images));

        CreateMap<Domain.Product.Product, AdminProductViewDto>()
            .ForMember(dest => dest.CategoryGroup, opt => opt.MapFrom(src => src.CategoryGroup != null ? new { src.CategoryGroup.Id, src.CategoryGroup.Name, CategoryName = src.CategoryGroup.Category.Name } : null))
            .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants))
            .ForMember(dest => dest.Images, opt => opt.MapFrom(src => src.Images));

        CreateMap<Domain.Product.ProductVariant, ProductVariantResponseDto>()
            .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.VariantAttributes.ToDictionary(
                    va => va.AttributeValue.AttributeType.Name.ToLower(),
                    va => new AttributeValueDto(va.AttributeValueId, va.AttributeValue.AttributeType.Name, va.AttributeValue.AttributeType.DisplayName, va.AttributeValue.Value, va.AttributeValue.DisplayValue, va.AttributeValue.HexCode)
                )))
            .ForMember(dest => dest.Images, opt => opt.Ignore());

        CreateMap<Domain.Media.Media, MediaDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore());

        CreateMap<Domain.User.User, UserProfileDto>()
            .ForMember(dest => dest.Addresses, opt => opt.MapFrom(src => src.UserAddresses));

        CreateMap<Domain.User.UserAddress, UserAddressDto>();

        CreateMap<UpdateProfileDto, Domain.User.User>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName));

        CreateMap<Domain.Product.ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => $"{src.User.FirstName} {src.User.LastName}"));
    }
}