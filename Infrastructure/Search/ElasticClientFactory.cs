namespace Infrastructure.Search;

public static class ElasticClientFactory
{
    public static ElasticsearchClient Create(IConfiguration configuration, ILogger logger)
    {
        var url = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        var username = configuration["Elasticsearch:Username"];
        var password = configuration["Elasticsearch:Password"];
        var cloudId = configuration["Elasticsearch:CloudId"];
        var certificateFingerprint = configuration["Elasticsearch:CertificateFingerprint"];
        var enableCloudId = configuration.GetValue<bool>("Elasticsearch:EnableCloudId", false);
        var timeout = configuration.GetValue<int>("Elasticsearch:Timeout", 60);
        var requestTimeout = configuration.GetValue<int>("Elasticsearch:RequestTimeout", 30);
        var pingTimeout = configuration.GetValue<int>("Elasticsearch:PingTimeout", 5);
        var enableDebugMode = configuration.GetValue<bool>("Elasticsearch:EnableDebugMode", false);
        var maxRetries = configuration.GetValue<int>("Elasticsearch:MaxRetries", 3);

        ElasticsearchClientSettings settings;

        if (enableCloudId && !string.IsNullOrEmpty(cloudId))
        {
            settings = new ElasticsearchClientSettings(new Uri("http://localhost:9200"));

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                settings = settings.Authentication(new BasicAuthentication(username, password));
            }

            if (!string.IsNullOrEmpty(certificateFingerprint))
            {
                settings = settings.CertificateFingerprint(certificateFingerprint);
            }
        }
        else
        {
            var uri = new Uri(url);
            settings = new ElasticsearchClientSettings(uri);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                settings = settings.Authentication(new BasicAuthentication(username, password));
            }
        }

        settings = settings
            .DefaultIndex("products_v1")
            .RequestTimeout(TimeSpan.FromSeconds(requestTimeout))
            .PingTimeout(TimeSpan.FromSeconds(pingTimeout))
            .MaximumRetries(maxRetries)
            .MaxRetryTimeout(TimeSpan.FromSeconds(timeout))
            .EnableHttpCompression(true)
            .ThrowExceptions(false);

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
                    "Elasticsearch request failed: {Method} {Uri} - Status: {Status}, Debug: {Debug}",
                    details.HttpMethod,
                    details.Uri,
                    details.HttpStatusCode,
                    details.DebugInformation);
            }
            else if (enableDebugMode)
            {
                logger.LogDebug(
                    "Elasticsearch request completed: {Method} {Uri} - Status: {Status}, ContentType: {ContentType}",
                    details.HttpMethod,
                    details.Uri,
                    details.HttpStatusCode,
                    details.ResponseContentType);
            }
        });

        var client = new ElasticsearchClient(settings);

        ValidateConnection(client, logger);

        return client;
    }

    private static void ValidateConnection(ElasticsearchClient client, ILogger logger)
    {
        try
        {
            var pingResponse = client.Ping();

            if (pingResponse.IsValidResponse)
            {
                logger.LogInformation("Successfully connected to Elasticsearch");
            }
            else
            {
                logger.LogError(
                    "Failed to connect to Elasticsearch: {Error}",
                    pingResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while validating Elasticsearch connection");
        }
    }

    public static async Task<ElasticsearchClient> CreateAsync(
        IConfiguration configuration,
        ILogger logger,
        CancellationToken ct = default)
    {
        var client = Create(configuration, logger);

        try
        {
            var pingResponse = await client.PingAsync(ct);

            if (pingResponse.IsValidResponse)
            {
                logger.LogInformation("Successfully connected to Elasticsearch (async)");
            }
            else
            {
                logger.LogError(
                    "Failed to connect to Elasticsearch (async): {Error}",
                    pingResponse.DebugInformation);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while validating Elasticsearch connection (async)");
        }

        return client;
    }
}