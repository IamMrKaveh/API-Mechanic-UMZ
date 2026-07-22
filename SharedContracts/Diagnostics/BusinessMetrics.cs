using System.Diagnostics.Metrics;

namespace SharedContracts.Diagnostics;

public sealed class BusinessMetrics : IDisposable
{
    public const string MeterName = "Mechanic.Business";
    public const string MeterVersion = "1.0";

    private readonly Meter _meter;

    public Counter<long> OrdersPlacedTotal { get; }
    public Counter<long> PaymentsVerifiedTotal { get; }
    public Histogram<double> WalletDebitAmount { get; }
    public Counter<long> OtpSentTotal { get; }
    public Counter<long> SagaStateTransitionsTotal { get; }
    public Counter<long> UnitOfWorkBypassStrategyTotal { get; }
    public Counter<long> RateLimitFallbackActive { get; }

    public BusinessMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName, MeterVersion);

        OrdersPlacedTotal = _meter.CreateCounter<long>(
            "orders_placed_total",
            unit: "orders",
            description: "Number of orders placed.");

        PaymentsVerifiedTotal = _meter.CreateCounter<long>(
            "payments_verified_total",
            unit: "payments",
            description: "Number of payment verification attempts, tagged by status.");

        WalletDebitAmount = _meter.CreateHistogram<double>(
            "wallet_debit_amount",
            unit: "IRT",
            description: "Distribution of wallet debit amounts.");

        OtpSentTotal = _meter.CreateCounter<long>(
            "otp_sent_total",
            unit: "otp",
            description: "Number of OTP send attempts, tagged by result.");

        SagaStateTransitionsTotal = _meter.CreateCounter<long>(
            "saga_state_transitions_total",
            unit: "transitions",
            description: "Number of saga state transitions, tagged by from/to.");

        UnitOfWorkBypassStrategyTotal = _meter.CreateCounter<long>(
            "unit_of_work_bypass_strategy_total",
            unit: "events",
            description: "Number of times a UnitOfWork call bypassed the execution strategy.");

        RateLimitFallbackActive = _meter.CreateCounter<long>(
            "ratelimit_fallback_active",
            unit: "events",
            description: "Number of times rate-limit fell back to in-memory implementation.");
    }

    public void Dispose() => _meter.Dispose();
}
