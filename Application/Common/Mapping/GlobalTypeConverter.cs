using Application.Common.Mapping.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Mapster;

namespace Application.Common.Mapping;

public class GlobalTypeConverter : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Guid, string>().MapWith(src => src.ToString());

        config.NewConfig<string, Guid>()
            .MapWith(src => Guid.Parse(src));

        config.NewConfig<DateTime, string>().MapWith(src => src.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        config.NewConfig<Money, MoneyDto>()
            .MapWith(src => new MoneyDto(src.Amount, src.Currency));

        config.NewConfig<Percentage, PercentageDto>()
            .MapWith(src => new PercentageDto(src.Value));

        config.NewConfig<string, Slug>()
    .MapWith(src => string.IsNullOrWhiteSpace(src)
        ? null!
        : Slug.FromString(src));

        config.NewConfig<string, ProductName>()
            .MapWith(src => ProductName.Create(src));

        config.NewConfig<Guid, CategoryId>()
            .MapWith(src => CategoryId.From(src));

        config.NewConfig<Guid, BrandId>()
            .MapWith(src => BrandId.From(src));

        config.NewConfig<decimal, Money>()
            .MapWith(src => Money.Create(src, "IRT"));

        config.NewConfig<Money, decimal>()
            .MapWith(src => src.Amount);
    }
}