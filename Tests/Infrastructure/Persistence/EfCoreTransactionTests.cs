using System.Reflection;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Tests.Infrastructure.Persistence;

public class EfCoreTransactionTests
{
    private static readonly Type TransactionType =
        typeof(DBContext).Assembly.GetType("Infrastructure.Persistence.EfCoreTransaction")!;

    private static ITransaction Create(IDbContextTransaction inner)
    {
        TransactionType.ShouldNotBeNull();
        return (ITransaction)Activator.CreateInstance(TransactionType, inner)!;
    }

    private static IDbContextTransaction InnerOf(ITransaction transaction) =>
        (IDbContextTransaction)TransactionType.GetProperty("Inner")!.GetValue(transaction)!;

    [Fact]
    public void Type_IsSealedAndImplementsTransactionContract()
    {
        TransactionType.IsSealed.ShouldBeTrue();
        typeof(ITransaction).IsAssignableFrom(TransactionType).ShouldBeTrue();
    }

    [Fact]
    public void Inner_ExposesWrappedTransaction()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        InnerOf(transaction).ShouldBeSameAs(inner);
    }

    [Fact]
    public void Dispose_DelegatesToInnerExactlyOnce()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        transaction.Dispose();

        inner.Received(1).Dispose();
    }

    [Fact]
    public void Dispose_WhenCalledTwice_DisposesInnerOnlyOnce()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        transaction.Dispose();
        transaction.Dispose();

        inner.Received(1).Dispose();
    }

    [Fact]
    public async Task DisposeAsync_DelegatesToInnerExactlyOnce()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        await transaction.DisposeAsync();

        await inner.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledTwice_DisposesInnerOnlyOnce()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        await transaction.DisposeAsync();
        await transaction.DisposeAsync();

        await inner.Received(1).DisposeAsync();
    }

    [Fact]
    public async Task DisposeFollowedByDisposeAsync_DisposesInnerOnlyOnce()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        transaction.Dispose();
        await transaction.DisposeAsync();

        inner.Received(1).Dispose();
        await inner.DidNotReceive().DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsyncFollowedByDispose_DisposesInnerOnlyOnce()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        var transaction = Create(inner);

        await transaction.DisposeAsync();
        transaction.Dispose();

        await inner.Received(1).DisposeAsync();
        inner.DidNotReceive().Dispose();
    }

    [Fact]
    public void Dispose_WhenInnerThrows_PropagatesException()
    {
        var inner = Substitute.For<IDbContextTransaction>();
        inner.When(x => x.Dispose()).Do(_ => throw new InvalidOperationException("dispose failed"));
        var transaction = Create(inner);

        Should.Throw<InvalidOperationException>(() => transaction.Dispose());
    }

    [Fact]
    public void Constructor_AcceptsSingleInnerTransactionParameter()
    {
        var constructor = TransactionType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(c => c.GetParameters().Length == 1);

        constructor.GetParameters()[0].ParameterType.ShouldBe(typeof(IDbContextTransaction));
    }
}
