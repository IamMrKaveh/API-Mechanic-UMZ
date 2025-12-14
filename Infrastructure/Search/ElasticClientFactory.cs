namespace Infrastructure.Search;

public static class ElasticClientFactory
{
    public static ElasticsearchClient Create(IConfiguration config, ILogger<ElasticsearchClient> logger)
    {
        var url = config["Elasticsearch:Url"] ?? Environment.GetEnvironmentVariable("ELASTIC_URL");
        var username = config["Elasticsearch:Username"] ?? Environment.GetEnvironmentVariable("ELASTIC_USERNAME");
        var password = config["Elasticsearch:Password"] ?? Environment.GetEnvironmentVariable("ELASTIC_PASSWORD");
        var apiKey = config["Elasticsearch:ApiKey"] ?? Environment.GetEnvironmentVariable("ELASTIC_APIKEY");

        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url), "Elasticsearch Url is not configured");

        var uri = new Uri(url);
        var defaultIndex = config["Elasticsearch:Index"];

        var settings = new ElasticsearchClientSettings(uri)
            .DefaultIndex(defaultIndex ?? "products_v1")
            .RequestTimeout(TimeSpan.FromSeconds(config.GetValue<int>("Elasticsearch:TimeoutSeconds", 60)))
            .MaximumRetries(config.GetValue<int>("Elasticsearch:MaxRetries", 5))
            .ThrowExceptions(false)
            .EnableHttpCompression()
            .OnRequestCompleted(details =>
            {
                if (details.HttpStatusCode != null && details.HttpStatusCode >= 400)
                {
                    var duration = details.ResponseContentType != null ?
                        (DateTime.UtcNow - (details.AuditTrail?.FirstOrDefault()?.Started ?? DateTime.UtcNow)).TotalMilliseconds : 0;

                    logger.LogWarning(
                        "Elasticsearch request failed: {Method} {Path} - Status: {Status}, Duration: {Duration}ms",
                        details.HttpMethod,
                        details.Uri?.AbsolutePath ?? "unknown",
                        details.HttpStatusCode,
                        duration);
                }
                else if (details.ResponseContentType != null)
                {
                    var duration = (DateTime.UtcNow - (details.AuditTrail?.FirstOrDefault()?.Started ?? DateTime.UtcNow)).TotalMilliseconds;
                    if (duration > 1000)
                    {
                        logger.LogWarning(
                            "Slow Elasticsearch query: {Method} {Path} - Duration: {Duration}ms",
                            details.HttpMethod,
                            details.Uri?.AbsolutePath ?? "unknown",
                            duration);
                    }
                }
            });

        var environment = config["ASPNETCORE_ENVIRONMENT"];
        if (environment == "Development")
        {
            settings.EnableDebugMode().PrettyJson();
        }

        if (!string.IsNullOrEmpty(apiKey))
        {
            settings.Authentication(new Elastic.Transport.ApiKey(apiKey));
            logger.LogInformation("Elasticsearch configured with API Key authentication");
        }
        else if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            settings.Authentication(new Elastic.Transport.BasicAuthentication(username, password));
            logger.LogInformation("Elasticsearch configured with Basic authentication");
        }
        else
        {
            logger.LogWarning("Elasticsearch authentication not configured");
        }

        var validateCertificate = config.GetValue<bool>("Elasticsearch:ValidateCertificate", true);

        if (!validateCertificate)
        {
            logger.LogWarning(
                "SECURITY WARNING: SSL certificate validation is being disabled. Environment: {Environment}",
                environment);
        }

        if (environment == "Production")
        {
            if (!validateCertificate)
            {
                logger.LogCritical(
                    "SECURITY VIOLATION: Attempt to disable SSL certificate validation in Production");
                throw new InvalidOperationException(
                    "SSL certificate validation cannot be disabled in Production environment");
            }

            logger.LogInformation("SSL certificate validation is ENABLED (Production environment)");
        }
        else if (environment == "Staging")
        {
            if (!validateCertificate)
            {
                logger.LogCritical(
                    "SECURITY VIOLATION: Attempt to disable SSL certificate validation in Staging");
                throw new InvalidOperationException(
                    "SSL certificate validation cannot be disabled in Staging environment");
            }
        }
        else if (environment == "Development")
        {
            if (!validateCertificate)
            {
                logger.LogWarning(
                    "SSL certificate validation is DISABLED - Development mode only. " +
                    "This configuration is NOT suitable for production use.");

                settings.ServerCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
                    {
                        logger.LogDebug(
                            "SSL certificate validation skipped in Development. Errors: {Errors}",
                            sslPolicyErrors);
                    }
                    return true;
                });
            }
        }

        return new ElasticsearchClient(settings);
    }
}