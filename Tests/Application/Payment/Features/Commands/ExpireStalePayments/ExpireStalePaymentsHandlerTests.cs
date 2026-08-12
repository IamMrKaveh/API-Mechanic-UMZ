using Application.Payment.Features.Commands.ExpireStalePayments;
using Domain.Payment.Interfaces;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using PaymentTransactions = Domain.Payment.Aggregates.PaymentTransaction;

namespace Tests.Application.Payment.Features.Commands.ExpireStalePayments;

public class ExpireStalePaymentsHandlerTests
{
    private readonly IPaymentTransactionRepository _paymentRepository = Substitute.For<IPaymentTransactionRepository>(); private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>(); private readonly ExpireStalePaymentsHandler _sut;

    public ExpireStalePaymentsHandlerTests()
    {
        _sut = new ExpireStalePaymentsHandler(_paymentRepository, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenNoExpiredTransactions_ReturnsZeroAndDoesNotUpdate()
    {
        var now = DateTime.UtcNow;
        _dateTimeProvider.UtcNow.Returns(now);
        _paymentRepository
            .GetPendingExpiredTransactionsAsync(now, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<PaymentTransactions>());

        var result = await _sut.Handle(new ExpireStalePaymentsCommand(now), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);
        _paymentRepository.DidNotReceive().Update(Arg.Any<PaymentTransactions>());
    }

    [Fact]
    public async Task Handle_WhenTransactionsExpired_ExpiresEachAndUpdatesRepository()
    {
        var now = DateTime.UtcNow;
        var pastNow = now.AddHours(-2);

        var tx1 = new PaymentTransactionBuilder().WithNow(pastNow).WithExpiryMinutes(20).Build();
        var tx2 = new PaymentTransactionBuilder().WithNow(pastNow).WithExpiryMinutes(20).Build();

        tx1.IsPending().ShouldBeTrue();
        tx2.IsPending().ShouldBeTrue();

        _dateTimeProvider.UtcNow.Returns(now);
        _paymentRepository
            .GetPendingExpiredTransactionsAsync(now, Arg.Any<CancellationToken>())
            .Returns(new[] { tx1, tx2 });

        var result = await _sut.Handle(new ExpireStalePaymentsCommand(now), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(2);
        tx1.IsPending().ShouldBeFalse();
        tx2.IsPending().ShouldBeFalse();
        _paymentRepository.Received(1).Update(tx1);
        _paymentRepository.Received(1).Update(tx2);
    }

    [Fact]
    public async Task Handle_WhenPendingTransactionIsNotYetExpired_DoesNotUpdate()
    {
        var now = DateTime.UtcNow;
        var future = now.AddHours(1);

        var tx = new PaymentTransactionBuilder().WithNow(future).WithExpiryMinutes(20).Build();

        _dateTimeProvider.UtcNow.Returns(now);
        _paymentRepository
            .GetPendingExpiredTransactionsAsync(now, Arg.Any<CancellationToken>())
            .Returns(new[] { tx });

        var result = await _sut.Handle(new ExpireStalePaymentsCommand(now), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);
        tx.IsPending().ShouldBeTrue();
        _paymentRepository.DidNotReceive().Update(Arg.Any<PaymentTransactions>());
    }
}
