using Application.Audit.Contracts;
using Application.Search.Contracts;
using Application.Search.Features.Shared;
using Elastic.Clients.Elasticsearch;

namespace Infrastructure.Search;

public class ElasticIndexManager(
    ElasticsearchClient client,
    IAuditService auditService,
    IConfiguration configuration) : IElasticIndexManager
{
    public async Task<bool> CreateProductIndexAsync(CancellationToken ct = default)
    {
        try
        {
            var indexName = "products_v1";
            var existsResponse = await client.Indices.ExistsAsync(indexName, ct);

            if (existsResponse.Exists)
            {
                await auditService.LogInformationAsync($"Index {indexName} already exists", ct);
                return true;
            }

            var response = await client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(configuration.GetValue<int>("Elasticsearch:NumberOfShards", 1))
                    .NumberOfReplicas(configuration.GetValue<int>("Elasticsearch:NumberOfReplicas", 0))
                    .RefreshInterval(5000)
                    .MaxResultWindow(10000)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_filter", m => m
                                .Mappings(new[]
                                {
                                    "٠ => 0", "١ => 1", "٢ => 2", "٣ => 3", "٤ => 4",
                                    "٥ => 5", "٦ => 6", "٧ => 7", "٨ => 8", "٩ => 9",
                                    "ي => ی", "ك => ک", "‌ =>  ", "ة => ه", "ۀ => ه",
                                    "ً => ", "ٌ => ", "ٍ => ", "َ => ", "ُ => ", "ِ => ", "ّ => ", "ْ => "
                                })
                            )
                        )
                        .TokenFilters(tf => tf
                            .Stop("persian_stop", st => st
                                .Stopwords(new[]
                                {
                                    "و", "در", "به", "از", "که", "این", "است", "را", "با",
                                    "برای", "آن", "یک", "شود", "شده", "خود", "های", "شد",
                                    "یا", "تا", "کند", "بر", "بود", "هم", "نیز", "روی"
                                })
                            )
                            .Stemmer("persian_stemmer", st => st.Language("persian"))
                            .EdgeNGram("edge_ngram_filter", en => en.MinGram(2).MaxGram(15))
                        )
                        .Analyzers(an => an
                            .Custom("persian_advanced", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit", "persian_stop", "persian_stemmer" })
                            )
                            .Custom("persian_autocomplete", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit", "persian_stop", "edge_ngram_filter" })
                            )
                            .Custom("persian_autocomplete_search", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit", "persian_stop" })
                            )
                        )
                        .Normalizers(n => n
                            .Custom("persian_normalizer", cn => cn
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase" })
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties<ProductSearchDocument>(p => p
                        .IntegerNumber(n => n.ProductId)
                        .Text(t => t.Name, td => td
                            .Analyzer("persian_advanced")
                            .SearchAnalyzer("persian_advanced")
                        )
                        .Text(t => t.Description, td => td
                            .Analyzer("persian_advanced")
                            .SearchAnalyzer("persian_advanced")
                        )
                        .Keyword(k => k.Slug)
                        .Keyword(k => k.Sku)
                        .Text(t => t.CategoryName, td => td
                            .Analyzer("persian_advanced")
                        )
                        .IntegerNumber(n => n.CategoryId)
                        .Text(t => t.BrandName, td => td
                            .Analyzer("persian_advanced")
                        )
                        .IntegerNumber(n => n.BrandId)
                        .FloatNumber(n => n.Price)
                        .Keyword(k => k.Images)
                        .Keyword(k => k.ImageUrl)
                        .Boolean(b => b.IsActive)
                        .Boolean(b => b.InStock)
                        .IntegerNumber(n => n.StockQuantity)
                        .Date(d => d.CreatedAt)
                        .Date(d => d.UpdatedAt)
                        .FloatNumber(n => n.AverageRating)
                        .IntegerNumber(n => n.ReviewCount)
                        .IntegerNumber(n => n.SalesCount)
                        .Keyword(k => k.Tags)
                    )
                ), ct);

            if (response.IsValidResponse)
            {
                await auditService.LogInformationAsync($"Successfully created index {indexName}", ct);
                return true;
            }

            await auditService.LogErrorAsync($"Failed to create index {indexName}: {response.DebugInformation}", ct);
            return false;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"Exception while creating product index: {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> CreateCategoryIndexAsync(CancellationToken ct = default)
    {
        try
        {
            var indexName = "categories_v1";
            var response = await client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                    .Analysis(a => a
                        .CharFilters(cf => cf
                            .Mapping("persian_char_filter", m => m
                                .Mappings(new[] { "ي => ی", "ك => ک", "‌ =>  ", "ة => ه", "ۀ => ه" })
                            )
                        )
                        .Analyzers(an => an
                            .Custom("persian_advanced", ca => ca
                                .Tokenizer("standard")
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase", "decimal_digit" })
                            )
                        )
                        .Normalizers(n => n
                            .Custom("persian_normalizer", cn => cn
                                .CharFilter(new[] { "persian_char_filter" })
                                .Filter(new[] { "lowercase" })
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties<CategorySearchDocument>(p => p
                        .IntegerNumber(n => n.CategoryId)
                        .Text(t => t.Name, td => td.Analyzer("persian_advanced"))
                        .Keyword(k => k.Slug)
                        .Boolean(b => b.IsActive)
                        .IntegerNumber(n => n.ProductCount)
                    )
                ), ct);

            if (response.IsValidResponse)
            {
                await auditService.LogInformationAsync($"Successfully created index {indexName}", ct);
                return true;
            }

            await auditService.LogErrorAsync($"Failed to create index {indexName}: {response.DebugInformation}", ct);
            return false;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"Exception while creating category index: {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> CreateBrandIndexAsync(CancellationToken ct = default)
    {
        try
        {
            var indexName = "brands_v1";
            var response = await client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                )
                .Mappings(m => m
                    .Properties<BrandSearchDocument>(p => p
                        .IntegerNumber(n => n.BrandId)
                        .Text(t => t.Name)
                        .Keyword(k => k.Slug)
                        .Boolean(b => b.IsActive)
                        .IntegerNumber(n => n.ProductCount)
                    )
                ), ct);

            if (response.IsValidResponse)
            {
                await auditService.LogInformationAsync($"Successfully created index {indexName}", ct);
                return true;
            }

            await auditService.LogErrorAsync($"Failed to create index {indexName}: {response.DebugInformation}", ct);
            return false;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"Exception while creating brand index: {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var response = await client.Indices.DeleteAsync(indexName, ct);
            if (response.IsValidResponse)
            {
                await auditService.LogInformationAsync($"Successfully deleted index {indexName}", ct);
                return true;
            }

            await auditService.LogErrorAsync($"Failed to delete index {indexName}: {response.DebugInformation}", ct);
            return false;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"Exception while deleting index {indexName}: {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken ct = default)
    {
        try
        {
            var response = await client.Indices.ExistsAsync(indexName, ct);
            return response.Exists;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ReindexAsync(string sourceIndex, string destinationIndex, CancellationToken ct = default)
    {
        try
        {
            var response = await client.ReindexAsync(r => r
                .Source(s => s.Indices(sourceIndex))
                .Dest(d => d.Index(destinationIndex)), ct);

            if (response.IsValidResponse)
            {
                await auditService.LogInformationAsync($"Reindex from {sourceIndex} to {destinationIndex} succeeded", ct);
                return true;
            }

            await auditService.LogErrorAsync($"Reindex failed: {response.DebugInformation}", ct);
            return false;
        }
        catch (Exception ex)
        {
            await auditService.LogErrorAsync($"Exception during reindex: {ex.Message}", ct);
            return false;
        }
    }

    public async Task<bool> CreateAllIndicesAsync(CancellationToken ct = default)
    {
        var product = await CreateProductIndexAsync(ct);
        var category = await CreateCategoryIndexAsync(ct);
        var brand = await CreateBrandIndexAsync(ct);
        return product && category && brand;
    }
}