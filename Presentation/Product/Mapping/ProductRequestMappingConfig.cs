using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Mapster;
using Presentation.Product.Requests;

namespace Presentation.Product.Mapping;

public sealed class ProductRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateProductRequest, CreateProductCommand>()
            .Ignore(dest => dest.CreatedByUserId);

        config.NewConfig<UpdateProductRequest, UpdateProductCommand>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UpdatedByUserId);
    }
}