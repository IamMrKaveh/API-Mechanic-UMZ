namespace Infrastructure.Search;

public static class ElasticClientFactory
{
    public static ElasticsearchClient Create(
        IConfiguration configuration,
        ILogger logger)
    {
        var url = configuration["Elasticsearch:Url"]
            ?? throw new InvalidOperationException("Elasticsearch Url is not configured");

        var username = configuration["Elasticsearch:Username"];
        var password = configuration["Elasticsearch:Password"];

        var requestTimeout = configuration.GetValue<int>("Elasticsearch:RequestTimeout", 30);
        var pingTimeout = configuration.GetValue<int>("Elasticsearch:PingTimeout", 5);
        var maxRetries = configuration.GetValue<int>("Elasticsearch:MaxRetries", 3);
        var enableDebugMode = configuration.GetValue<bool>("Elasticsearch:EnableDebugMode", false);

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

        if (enableDebugMode)
        {
            settings = settings
                .DisableDirectStreaming()
                .EnableDebugMode()
                .PrettyJson();
        }

        settings = settings.OnRequestCompleted(details =>
        {
            if (details.HttpStatusCode >= 400)
            {
                logger.LogWarning(
                    "Elasticsearch request failed: {Method} {Uri} - {Status}",
                    details.HttpMethod,
                    details.Uri,
                    details.HttpStatusCode);
            }
        });

        return new ElasticsearchClient(settings);
    }
}