using Application.Audit.Contracts;

namespace Infrastructure.Search;

public sealed class ElasticsearchCircuitBreaker
{
    private readonly IAuditService _auditService;
    private readonly int _failureThreshold;
    private readonly TimeSpan _breakDuration;

    private int _failureCount;
    private DateTime? _openedAt;
    private CircuitState _state = CircuitState.Closed;

    private enum CircuitState
    { Closed, Open, HalfOpen }

    public ElasticsearchCircuitBreaker(IAuditService auditService, IConfiguration configuration)
    {
        _auditService = auditService;
        _failureThreshold = configuration.GetValue("Elasticsearch:CircuitBreaker:FailureThreshold", 5);
        _breakDuration = TimeSpan.FromSeconds(
            configuration.GetValue("Elasticsearch:CircuitBreaker:BreakDurationSeconds", 60));
    }

    public bool IsAllowed()
    {
        if (_state == CircuitState.Closed)
            return true;

        if (_state == CircuitState.Open)
        {
            if (_openedAt.HasValue && DateTime.UtcNow - _openedAt.Value >= _breakDuration)
            {
                _state = CircuitState.HalfOpen;
                _ = _auditService.LogWarningAsync("Circuit breaker half-open for Elasticsearch", CancellationToken.None);
                return true;
            }

            return false;
        }

        return true;
    }

    public void RecordSuccess()
    {
        _failureCount = 0;
        _openedAt = null;
        _state = CircuitState.Closed;
    }

    public void RecordFailure()
    {
        _failureCount++;

        if (_failureCount >= _failureThreshold && _state != CircuitState.Open)
        {
            _state = CircuitState.Open;
            _openedAt = DateTime.UtcNow;
            _ = _auditService.LogErrorAsync(
                $"Circuit breaker opened for Elasticsearch after {_failureCount} failures",
                CancellationToken.None);
        }
    }
}