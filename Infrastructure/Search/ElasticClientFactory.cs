using Elastic.Clients.Elasticsearch;
using Infrastructure.Search.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Search;

public static class ElasticClientFactory
{
    public static ElasticsearchClient Create(ElasticsearchOptions options)
    {
        var settings = new ElasticsearchClientSettings(
            new Uri(options.Urls.FirstOrDefault() ?? "http://localhost:9200"))
            .RequestTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds))
            .MaximumRetries(options.MaxRetries);

        if (!string.IsNullOrWhiteSpace(options.Username) &&
            !string.IsNullOrWhiteSpace(options.Password))
        {
            settings = settings.Authentication(
                new Elastic.Transport.BasicAuthentication(options.Username, options.Password));
        }

        if (options.DebugMode)
            settings = settings.EnableDebugMode();

        return new ElasticsearchClient(settings);
    }

    public static ElasticsearchClient Create(IOptions<ElasticsearchOptions> options)
        => Create(options.Value);
}