namespace Infrastructure.Search.Options;

public sealed class ElasticsearchIndexOptions
{
    public string Products { get; init; } = "products_v1";
    public string Categories { get; init; } = "categories_v1";
    public string Brands { get; init; } = "brands_v1";
}