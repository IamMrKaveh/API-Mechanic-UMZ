using Infrastructure.Search;

namespace Tests.Infrastructure.Search;

public class ElasticsearchCircuitBreakerTests
{
    [Fact]
    public void IsAllowed_WhenNoFailuresRecorded_ReturnsTrue()
    {
        var auditService = Substitute.For<IAuditService>(); var configuration = BuildConfiguration(failureThreshold: 3, breakDurationSeconds: 60); var sut = new ElasticsearchCircuitBreaker(auditService, configuration);

        var allowed = sut.IsAllowed();

        allowed.ShouldBeTrue();
    }

    [Fact]
    public void RecordFailure_BelowThreshold_KeepsCircuitClosedAndIsAllowedRemainsTrue()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 3, breakDurationSeconds: 60);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);

        sut.RecordFailure();
        sut.RecordFailure();

        sut.IsAllowed().ShouldBeTrue();
        auditService.DidNotReceive().LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RecordFailure_ReachingThreshold_OpensCircuitAndLogsErrorViaAuditService()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 2, breakDurationSeconds: 3600);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);

        sut.RecordFailure();
        sut.RecordFailure();

        sut.IsAllowed().ShouldBeFalse();
        auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains("Circuit breaker opened for Elasticsearch")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RecordFailure_AfterCircuitAlreadyOpen_DoesNotLogAdditionalOpenError()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 1, breakDurationSeconds: 3600);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);

        sut.RecordFailure();
        sut.RecordFailure();
        sut.RecordFailure();

        auditService.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Circuit breaker opened for Elasticsearch")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void IsAllowed_WhenOpenAndBreakDurationNotElapsed_ReturnsFalse()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 1, breakDurationSeconds: 3600);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);
        sut.RecordFailure();

        var allowed = sut.IsAllowed();

        allowed.ShouldBeFalse();
        auditService.DidNotReceive().LogWarningAsync(
            Arg.Is<string>(s => s.Contains("half-open")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void IsAllowed_WhenOpenAndBreakDurationElapsed_TransitionsToHalfOpenAndReturnsTrueAndLogsWarning()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 1, breakDurationSeconds: 0);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);
        sut.RecordFailure();

        var allowed = sut.IsAllowed();

        allowed.ShouldBeTrue();
        auditService.Received(1).LogWarningAsync(
            Arg.Is<string>(s => s.Contains("Circuit breaker half-open for Elasticsearch")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RecordSuccess_AfterFailuresBelowThreshold_ResetsFailureCountSoThresholdIsNotReachedByNewFailure()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 2, breakDurationSeconds: 3600);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);
        sut.RecordFailure();
        sut.RecordSuccess();

        sut.RecordFailure();

        sut.IsAllowed().ShouldBeTrue();
        auditService.DidNotReceive().LogErrorAsync(
            Arg.Is<string>(s => s.Contains("Circuit breaker opened for Elasticsearch")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RecordSuccess_AfterCircuitOpened_ReclosesCircuitSoIsAllowedReturnsTrue()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 1, breakDurationSeconds: 3600);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);
        sut.RecordFailure();
        sut.IsAllowed().ShouldBeFalse();

        sut.RecordSuccess();

        sut.IsAllowed().ShouldBeTrue();
    }

    [Fact]
    public void RecordSuccess_WhenCircuitAlreadyClosed_KeepsIsAllowedTrueAndDoesNotEmitAuditLogs()
    {
        var auditService = Substitute.For<IAuditService>();
        var configuration = BuildConfiguration(failureThreshold: 3, breakDurationSeconds: 60);
        var sut = new ElasticsearchCircuitBreaker(auditService, configuration);

        sut.RecordSuccess();

        sut.IsAllowed().ShouldBeTrue();
        auditService.DidNotReceive().LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        auditService.DidNotReceive().LogWarningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static IConfiguration BuildConfiguration(int failureThreshold, int breakDurationSeconds)
    {
        var configuration = Substitute.For<IConfiguration>();

        var thresholdSection = Substitute.For<IConfigurationSection>();
        thresholdSection.Value.Returns(failureThreshold.ToString(CultureInfo.InvariantCulture));
        thresholdSection.Path.Returns("Elasticsearch:CircuitBreaker:FailureThreshold");
        configuration.GetSection("Elasticsearch:CircuitBreaker:FailureThreshold").Returns(thresholdSection);

        var durationSection = Substitute.For<IConfigurationSection>();
        durationSection.Value.Returns(breakDurationSeconds.ToString(CultureInfo.InvariantCulture));
        durationSection.Path.Returns("Elasticsearch:CircuitBreaker:BreakDurationSeconds");
        configuration.GetSection("Elasticsearch:CircuitBreaker:BreakDurationSeconds").Returns(durationSection);

        return configuration;
    }
}
