namespace Infrastructure.Search;

public class ElasticsearchMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _searchRequestCounter;
    private readonly Counter<long> _indexRequestCounter;
    private readonly Counter<long> _bulkRequestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Histogram<double> _searchDuration;
    private readonly Histogram<double> _indexDuration;
    private readonly Histogram<double> _bulkDuration;

    public ElasticsearchMetrics()
    {
        _meter = new Meter("Elasticsearch", "1.0");

        _searchRequestCounter = _meter.CreateCounter<long>(
            "elasticsearch.search.requests",
            description: "Number of search requests");

        _indexRequestCounter = _meter.CreateCounter<long>(
            "elasticsearch.index.requests",
            description: "Number of index requests");

        _bulkRequestCounter = _meter.CreateCounter<long>(
            "elasticsearch.bulk.requests",
            description: "Number of bulk requests");

        _errorCounter = _meter.CreateCounter<long>(
            "elasticsearch.errors",
            description: "Number of errors");

        _searchDuration = _meter.CreateHistogram<double>(
            "elasticsearch.search.duration",
            unit: "ms",
            description: "Duration of search requests");

        _indexDuration = _meter.CreateHistogram<double>(
            "elasticsearch.index.duration",
            unit: "ms",
            description: "Duration of index requests");

        _bulkDuration = _meter.CreateHistogram<double>(
            "elasticsearch.bulk.duration",
            unit: "ms",
            description: "Duration of bulk requests");
    }

    public void RecordSearchRequest(double durationMs, bool success, string? indexName = null)
    {
        _searchRequestCounter.Add(1,
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("index", indexName ?? "unknown"));

        _searchDuration.Record(durationMs,
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("index", indexName ?? "unknown"));

        if (!success)
        {
            _errorCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "search"),
                new KeyValuePair<string, object?>("index", indexName ?? "unknown"));
        }
    }

    public void RecordIndexRequest(double durationMs, bool success, string? indexName = null)
    {
        _indexRequestCounter.Add(1,
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("index", indexName ?? "unknown"));

        _indexDuration.Record(durationMs,
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("index", indexName ?? "unknown"));

        if (!success)
        {
            _errorCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "index"),
                new KeyValuePair<string, object?>("index", indexName ?? "unknown"));
        }
    }

    public void RecordBulkRequest(double durationMs, bool success, int itemCount, string? indexName = null)
    {
        _bulkRequestCounter.Add(1,
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("index", indexName ?? "unknown"),
            new KeyValuePair<string, object?>("item_count", itemCount));

        _bulkDuration.Record(durationMs,
            new KeyValuePair<string, object?>("success", success),
            new KeyValuePair<string, object?>("index", indexName ?? "unknown"),
            new KeyValuePair<string, object?>("item_count", itemCount));

        if (!success)
        {
            _errorCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "bulk"),
                new KeyValuePair<string, object?>("index", indexName ?? "unknown"));
        }
    }
}