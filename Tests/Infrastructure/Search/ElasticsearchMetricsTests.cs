using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Infrastructure.Search;

namespace Tests.Infrastructure.Search;

public sealed class ElasticsearchMetricsTests : IDisposable
{
    private readonly MeterListener _listener = new();
    private readonly ConcurrentBag<Measurement> _measurements = new();

    public sealed record Measurement(string Instrument, double Value, Dictionary<string, object?> Tags);

    public ElasticsearchMetricsTests()
    {
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Elasticsearch")
                listener.EnableMeasurementEvents(instrument);
        };
        _listener.SetMeasurementEventCallback<long>(OnLong);
        _listener.SetMeasurementEventCallback<double>(OnDouble);
        _listener.Start();
    }

    private void OnLong(Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _)
        => _measurements.Add(new Measurement(instrument.Name, value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value)));

    private void OnDouble(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? _)
        => _measurements.Add(new Measurement(instrument.Name, value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value)));

    public void Dispose() => _listener.Dispose();

    private IReadOnlyList<Measurement> For(string instrument) =>
        _measurements.Where(m => m.Instrument == instrument).ToList();

    [Fact]
    public void RecordSearchRequest_WhenSuccessful_EmitsRequestAndDurationWithoutError()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordSearchRequest(durationMs: 12.5, success: true, indexName: "products_v1");

        var requests = For("elasticsearch.search.requests");
        requests.Count.ShouldBe(1);
        requests[0].Value.ShouldBe(1);
        requests[0].Tags["success"].ShouldBe(true);
        requests[0].Tags["index"].ShouldBe("products_v1");

        var durations = For("elasticsearch.search.duration");
        durations.Count.ShouldBe(1);
        durations[0].Value.ShouldBe(12.5);

        For("elasticsearch.errors").ShouldBeEmpty();
    }

    [Fact]
    public void RecordSearchRequest_WhenFailed_EmitsErrorCounter()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordSearchRequest(durationMs: 3, success: false, indexName: "products_v1");

        var errors = For("elasticsearch.errors");
        errors.Count.ShouldBe(1);
        errors[0].Tags["operation"].ShouldBe("search");
        errors[0].Tags["index"].ShouldBe("products_v1");
    }

    [Fact]
    public void RecordSearchRequest_WithNullIndex_UsesUnknownLabel()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordSearchRequest(durationMs: 1, success: true, indexName: null);

        For("elasticsearch.search.requests")[0].Tags["index"].ShouldBe("unknown");
    }

    [Fact]
    public void RecordIndexRequest_WhenSuccessful_EmitsRequestAndDurationWithoutError()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordIndexRequest(durationMs: 7, success: true, indexName: "brands_v1");

        For("elasticsearch.index.requests").Count.ShouldBe(1);
        For("elasticsearch.index.duration").Count.ShouldBe(1);
        For("elasticsearch.errors").ShouldBeEmpty();
    }

    [Fact]
    public void RecordIndexRequest_WhenFailed_EmitsErrorCounter()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordIndexRequest(durationMs: 7, success: false, indexName: null);

        var errors = For("elasticsearch.errors");
        errors.Count.ShouldBe(1);
        errors[0].Tags["operation"].ShouldBe("index");
        errors[0].Tags["index"].ShouldBe("unknown");
    }

    [Fact]
    public void RecordBulkRequest_WhenSuccessful_EmitsRequestDurationAndItemCount()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordBulkRequest(durationMs: 44, success: true, itemCount: 25, indexName: "products_v1");

        var requests = For("elasticsearch.bulk.requests");
        requests.Count.ShouldBe(1);
        requests[0].Tags["item_count"].ShouldBe(25);
        requests[0].Tags["index"].ShouldBe("products_v1");
        For("elasticsearch.bulk.duration").Count.ShouldBe(1);
        For("elasticsearch.errors").ShouldBeEmpty();
    }

    [Fact]
    public void RecordBulkRequest_WhenFailed_EmitsErrorCounter()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordBulkRequest(durationMs: 44, success: false, itemCount: 25, indexName: "products_v1");

        For("elasticsearch.errors").Count.ShouldBe(1);
        For("elasticsearch.errors")[0].Tags["operation"].ShouldBe("bulk");
    }

    [Fact]
    public void RecordBulkOperationSuccess_EmitsSuccessCounterWithItemCount()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordBulkOperationSuccess(itemCount: 10, indexName: "categories_v1");

        var successes = For("elasticsearch.bulk.success");
        successes.Count.ShouldBe(1);
        successes[0].Value.ShouldBe(1);
        successes[0].Tags["item_count"].ShouldBe(10);
        successes[0].Tags["index"].ShouldBe("categories_v1");
    }

    [Fact]
    public void RecordBulkOperationFailure_EmitsFailureAndErrorCounters()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordBulkOperationFailure(indexName: "products_v1");

        For("elasticsearch.bulk.failures").Count.ShouldBe(1);
        var errors = For("elasticsearch.errors");
        errors.Count.ShouldBe(1);
        errors[0].Tags["operation"].ShouldBe("bulk_operation");
    }

    [Fact]
    public void RecordBulkOperationPartialFailure_EmitsPartialCounterWithFailedCount()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordBulkOperationPartialFailure(failedCount: 4, indexName: null);

        var partials = For("elasticsearch.bulk.partial_failures");
        partials.Count.ShouldBe(1);
        partials[0].Tags["failed_count"].ShouldBe(4);
        partials[0].Tags["index"].ShouldBe("unknown");
        For("elasticsearch.errors").ShouldBeEmpty();
    }

    [Fact]
    public void RecordMethods_WithEdgeValues_DoNotThrow()
    {
        var sut = new ElasticsearchMetrics();

        Should.NotThrow(() =>
        {
            sut.RecordSearchRequest(0, true);
            sut.RecordSearchRequest(-1, false, "");
            sut.RecordIndexRequest(0, true, "");
            sut.RecordBulkRequest(0, true, 0);
            sut.RecordBulkRequest(double.MaxValue, false, int.MaxValue, "x");
            sut.RecordBulkOperationSuccess(0);
            sut.RecordBulkOperationPartialFailure(0);
            sut.RecordBulkOperationFailure();
        });
    }

    [Fact]
    public void RecordSearchRequest_CalledRepeatedly_AccumulatesMeasurements()
    {
        var sut = new ElasticsearchMetrics();

        sut.RecordSearchRequest(1, true, "products_v1");
        sut.RecordSearchRequest(2, true, "products_v1");
        sut.RecordSearchRequest(3, false, "products_v1");

        For("elasticsearch.search.requests").Count.ShouldBe(3);
        For("elasticsearch.search.duration").Count.ShouldBe(3);
        For("elasticsearch.errors").Count.ShouldBe(1);
    }
}
