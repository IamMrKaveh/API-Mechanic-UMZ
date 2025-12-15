namespace Infrastructure.Search;

public static class ElasticClientFactory
{
    public static ElasticsearchClient Create(IConfiguration config, ILogger<ElasticsearchClient> logger)
    {
        var url = config["Elasticsearch:Url"] ?? Environment.GetEnvironmentVariable("ELASTIC_URL");

        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url), "Elasticsearch Url is not configured");

        var uri = new Uri(url);
        var defaultIndex = config["Elasticsearch:Index"];

        var settings = new ElasticsearchClientSettings(uri)
            .DefaultIndex(defaultIndex ?? "products_v1")
            .RequestTimeout(TimeSpan.FromSeconds(config.GetValue<int>("Elasticsearch:TimeoutSeconds", 60)))
            .MaximumRetries(config.GetValue<int>("Elasticsearch:MaxRetries", 5))
            .ThrowExceptions(false)
            .EnableHttpCompression();

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':');
            if (parts.Length == 2)
            {
                settings.Authentication(new Elastic.Transport.BasicAuthentication(parts[0], parts[1]));
                logger.LogInformation("Elasticsearch configured with Basic authentication from URI");
            }
        }
        else
        {
            var username = config["Elasticsearch:Username"] ?? Environment.GetEnvironmentVariable("ELASTIC_USERNAME");
            var password = config["Elasticsearch:Password"] ?? Environment.GetEnvironmentVariable("ELASTIC_PASSWORD");
            var apiKey = config["Elasticsearch:ApiKey"] ?? Environment.GetEnvironmentVariable("ELASTIC_APIKEY");

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
        }

        settings.OnRequestCompleted(details =>
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

        var validateCertificate = config.GetValue<bool>("Elasticsearch:ValidateCertificate", true);

        if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Elasticsearch connection is HTTP. SSL certificate validation skipped.");
        }
        else if (!validateCertificate)
        {
            logger.LogWarning(
                "SECURITY WARNING: SSL certificate validation is being disabled. Environment: {Environment}",
                environment);

            if (environment == "Production")
            {
                logger.LogCritical("SECURITY VIOLATION: SSL certificate validation disabled in Production via configuration.");
            }

            settings.ServerCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => true);
        }
        else
        {
            logger.LogInformation("SSL certificate validation is ENABLED");
        }

        return new ElasticsearchClient(settings);
    }
}