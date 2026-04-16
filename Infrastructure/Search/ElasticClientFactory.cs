using Infrastructure.Search.Options;

namespace Infrastructure.Search;

public static class ElasticClientFactory
{
    public static async Task<ElasticsearchClient> Create(
        IConfiguration configuration,
        IAuditService auditService)
    {
        var elasticOptions = configuration.GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        // بررسی فعال بودن Elasticsearch
        if (!elasticOptions.IsEnabled)
        {
            await auditService.LogWarningAsync("Elasticsearch is disabled in configuration. Client will not be functional.");
        }

        var url = elasticOptions.Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Elasticsearch Url is not configured");
        }

        var username = elasticOptions.Username;
        var password = elasticOptions.Password;
        var requestTimeout = elasticOptions.TimeoutSeconds;
        var pingTimeout = configuration.GetValue<int>("Elasticsearch:PingTimeout", 5);
        var maxRetries = elasticOptions.MaxRetries;
        var debugMode = elasticOptions.DebugMode;

        var settings = new ElasticsearchClientSettings(new Uri(url))
            .DefaultIndex("products_v1")
            .RequestTimeout(TimeSpan.FromSeconds(requestTimeout))
            .PingTimeout(TimeSpan.FromSeconds(pingTimeout))
            .MaximumRetries(maxRetries)
            .EnableHttpCompression(true)
            .ThrowExceptions();

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            settings = settings.Authentication(
                new BasicAuthentication(username, password));
        }

        if (debugMode)
        {
            settings = settings
                .DisableDirectStreaming()
                .EnableDebugMode()
                .PrettyJson();
        }

        settings = settings.OnRequestCompleted(async details =>
        {
            if (details.HttpStatusCode >= 400)
            {
                await auditService.LogErrorAsync("Elasticsearch request failed: {details.HttpMethod} {details.Uri} - {details.HttpStatusCode}");
            }
        });

        return new ElasticsearchClient(settings);
    }
}