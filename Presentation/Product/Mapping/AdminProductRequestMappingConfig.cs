using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Queries.GetAdminProductById;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Queries.GetAdminProducts;
using Mapster;
using Presentation.Product.Requests;

namespace Presentation.Product.Mapping;

public sealed class AdminProductRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAdminProductsRequest, GetAdminProductsQuery>()
            .Ignore(dest => dest.UserId);

        config.NewConfig<GetAdminProductByIdRequest, GetAdminProductByIdQuery>()
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.ProductId);

        config.NewConfig<GetAdminProductDetailRequest, GetAdminProductDetailQuery>()
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.ProductId);

        config.NewConfig<CreateProductRequest, CreateProductCommand>()
            .Ignore(dest => dest.UserId);

        config.NewConfig<UpdateProductRequest, UpdateProductCommand>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UserId);

        config.NewConfig<BulkUpdatePricesRequest, BulkUpdatePricesCommand>()
            .Ignore(dest => dest.UserId);

        config.NewConfig<DeleteProductRequest, DeleteProductCommand>()
            .Ignore(dest => dest.ProductId)
            .Ignore(dest => dest.UserId);

        config.NewConfig<ActiveProductRequest, ActivateProductCommand>()
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.ProductId);

        config.NewConfig<DeactiveProductRequest, DeactivateProductCommand>()
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.ProductId);

        config.NewConfig<RestoreProductRequest, RestoreProductCommand>()
            .Ignore(dest => dest.UserId)
            .Ignore(dest => dest.ProductId);
    }
}