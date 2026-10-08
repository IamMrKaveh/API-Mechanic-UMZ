using SharedContracts.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tests.SharedContracts.Diagnostics;

public class BusinessMetricsTests
{
    private static BusinessMetrics BuildMetrics(out TestMeterFactory factory)
    {
        factory = new TestMeterFactory();
        return new BusinessMetrics(factory);
    }

    [Fact]
    public void MeterConstants_HaveExpectedValues()
    {
        BusinessMetrics.MeterName.ShouldBe("Mechanic.Business");
        BusinessMetrics.MeterVersion.ShouldBe("1.0");
    }

    [Fact]
    public void Constructor_CreatesAllInstruments()
    {
        // Arrange & Act
        using var metrics = BuildMetrics(out _);

        // Assert
        metrics.OrdersPlacedTotal.ShouldNotBeNull();
        metrics.PaymentsVerifiedTotal.ShouldNotBeNull();
        metrics.WalletDebitAmount.ShouldNotBeNull();
        metrics.OtpSentTotal.ShouldNotBeNull();
        metrics.SagaStateTransitionsTotal.ShouldNotBeNull();
        metrics.UnitOfWorkBypassStrategyTotal.ShouldNotBeNull();
        metrics.RateLimitFallbackActive.ShouldNotBeNull();
    }

    [Fact]
    public void Instruments_CanRecordMeasurements_WithoutThrowing()
    {
        // Arrange
        using var metrics = BuildMetrics(out _);

        // Act
        var exception = Record.Exception(() =>
        {
            metrics.OrdersPlacedTotal.Add(1);
            metrics.PaymentsVerifiedTotal.Add(1, new KeyValuePair<string, object?>("status", "verified"));
            metrics.WalletDebitAmount.Record(50000);
            metrics.OtpSentTotal.Add(1);
            metrics.SagaStateTransitionsTotal.Add(1);
            metrics.UnitOfWorkBypassStrategyTotal.Add(1);
            metrics.RateLimitFallbackActive.Add(1);
        });

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var metrics = BuildMetrics(out _);

        // Act
        var exception = Record.Exception(() => metrics.Dispose());

        // Assert
        exception.ShouldBeNull();
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly Meter _meter = new(BusinessMetrics.MeterName, BusinessMetrics.MeterVersion);

        public Meter Create(string name)
            => _meter;

        public Meter Create(MeterOptions options)
            => _meter;

        public void Dispose()
            => _meter.Dispose();
    }
}
