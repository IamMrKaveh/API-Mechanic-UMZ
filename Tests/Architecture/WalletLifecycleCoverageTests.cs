using System.Reflection;
using Application.Common.Events;
using Domain.Common.Abstractions;
using ICommandContract = Application.Common.Interfaces.ICommand;
using DbContextType = Infrastructure.Persistence.Context.DBContext;
using WalletAggregate = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Architecture;

/// <summary>
/// The wallet events that carry a side effect must have a handler, otherwise the
/// money movement they describe silently never happens.
///
/// The other wallet events (reservation created, transfer completed, withdrawal
/// requested, ...) are applied synchronously by the command that raises them
/// and only record what happened, so they need no handler.
/// </summary>
public class WalletLifecycleCoverageTests
{
    private static readonly Assembly[] ApplicationAssemblies =
    [
        typeof(ICommandContract).Assembly,
        typeof(DbContextType).Assembly
    ];

    /// <summary>Every side effect of the wallet lifecycle and the handler that owns it.</summary>
    private static readonly (string Event, string Handler)[] SideEffects =
    [
        ("UserRegisteredEvent", "UserCreatedEventHandler"),
        ("PaymentSucceededEvent", "PaymentSucceededWalletCreditEventHandler"),
        ("PaymentRefundedEvent", "PaymentRefundedWalletEventHandler"),
        ("OrderCancelledEvent", "OrderCancelledWalletReleaseEventHandler"),
        ("WalletCreditedEvent", "PersistWalletLedgerOnCreditHandler"),
        ("WalletDebitedEvent", "PersistWalletLedgerOnDebitHandler"),
        ("WalletFrozenEvent", "FraudAlertCriticalFreezeHandler"),
        ("WalletUnfrozenEvent", "SendWalletUnfreezeNotificationHandler")
    ];

    [Fact]
    public void EverySideEffectingWalletEvent_HasAHandler()
    {
        var walletAssembly = typeof(WalletAggregate).Assembly;

        var missing = new List<string>();

        foreach (var (eventName, handlerName) in SideEffects)
        {
            var eventType = walletAssembly
                .GetTypes()
                .FirstOrDefault(t =>
                    t.Name == eventName
                    && typeof(IDomainEvent).IsAssignableFrom(t));

            if (eventType is null)
            {
                missing.Add($"{eventName} does not exist");
                continue;
            }

            if (!HasHandler(eventType))
                missing.Add($"{eventName} has no handler (expected {handlerName})");
        }

        missing.ShouldBeEmpty(
            "A side effect without a handler means the balance never moves.");
    }

    [Fact]
    public void TheWalletLifecycleSteps_AreCoveredByHandlers()
    {
        var handlerNames = ApplicationAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missing = SideEffects
            .Where(e => !handlerNames.Contains(e.Handler))
            .Select(e => $"{e.Event} is not handled by {e.Handler}")
            .ToList();

        missing.ShouldBeEmpty();
    }

    private static bool HasHandler(Type eventType)
    {
        var notificationInterface = typeof(INotificationHandler<>)
            .MakeGenericType(typeof(DomainEventNotification<>).MakeGenericType(eventType));

        return ApplicationAssemblies
            .SelectMany(a => a.GetTypes())
            .Any(t => t is { IsClass: true, IsAbstract: false }
                && notificationInterface.IsAssignableFrom(t));
    }
}
