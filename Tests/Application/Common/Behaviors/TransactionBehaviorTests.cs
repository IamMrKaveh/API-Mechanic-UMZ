using Application.Audit.Contracts;
using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Common.Behaviors;

public class TransactionBehaviorTests
{
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>(); private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private void ConfigureUnitOfWorkPassThrough<TResp>()
    {
        _uow.ExecuteStrategyAsync(
                Arg.Any<Func<CancellationToken, Task<TResp>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                var op = ci.Arg<Func<CancellationToken, Task<TResp>>>();
                var ct = ci.Arg<CancellationToken>();
                return await op(ct);
            });
    }

    [Fact]
    public async Task Handle_WhenRequestIsQuery_BypassesTransactionAndCallsNext()
    {
        var sut = new TransactionBehavior<TestQuery, ServiceResult<string>>(_uow, _audit);
        var invoked = false;

        var result = await sut.Handle(
            new TestQuery(),
            _ =>
            {
                invoked = true;
                return Task.FromResult(ServiceResult<string>.Success("v"));
            },
            CancellationToken.None);

        invoked.ShouldBeTrue();
        result.ShouldBeSuccess();
        await _uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenRequestBypassesTransactionBehavior_CallsNextWithoutSaving()
    {
        var sut = new TransactionBehavior<BypassCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new BypassCommand(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenRequestIsManualTransaction_CallsNextWithoutSaving()
    {
        var sut = new TransactionBehavior<ManualCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new ManualCommand(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsSuccess_CallsSaveChangesOnce()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult>();
        var sut = new TransactionBehavior<StandardCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new StandardCommand(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsFailure_DoesNotCallSaveChanges()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult>();
        var sut = new TransactionBehavior<StandardCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new StandardCommand(),
            _ => Task.FromResult(ServiceResult.Failure(Error.Validation("bad"))),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _uow.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolation_ReturnsConflictWithUniqueViolationCode()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult>();

        var pgEx = new PostgresException("duplicate", "ERROR", "ERROR", "23505");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new DbUpdateException("save failed", pgEx)));

        var sut = new TransactionBehavior<StandardCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new StandardCommand(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.UniqueViolation);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Message.ShouldBe("این رکورد از قبل وجود دارد.");
        await _audit.Received(1).LogSystemEventAsync(
            "UniqueConstraintViolation",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUniqueConstraintViolationWithMappedRequest_UsesMappedMessage()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult>();

        var pgEx = new PostgresException("duplicate", "ERROR", "ERROR", "23505");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new DbUpdateException("save failed", pgEx)));

        var sut = new TransactionBehavior<MappedUniqueCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new MappedUniqueCommand(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.UniqueViolation);
        result.Error.Message.ShouldBe("نام برند تکراری است.");
    }

    [Fact]
    public async Task Handle_WhenForeignKeyViolation_ReturnsConflictWithForeignKeyViolationCode()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult>();

        var pgEx = new PostgresException("fk", "ERROR", "ERROR", "23503");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new DbUpdateException("fk failed", pgEx)));

        var sut = new TransactionBehavior<StandardCommand, ServiceResult>(_uow, _audit);

        var result = await sut.Handle(
            new StandardCommand(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.ForeignKeyViolation);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Message.ShouldBe("این عملیات به دلیل وابستگی به منابع دیگر امکان‌پذیر نیست.");
        await _audit.Received(1).LogSystemEventAsync(
            "ForeignKeyViolation",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUnexpectedException_LogsAndRethrows()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult>();

        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new InvalidOperationException("boom")));

        var sut = new TransactionBehavior<StandardCommand, ServiceResult>(_uow, _audit);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.Handle(
                new StandardCommand(),
                _ => Task.FromResult(ServiceResult.Success()),
                CancellationToken.None));

        await _audit.Received(1).LogSystemEventAsync(
            "TransactionFailed",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithGenericServiceResult_ReturnsConflictOnUniqueViolation()
    {
        ConfigureUnitOfWorkPassThrough<ServiceResult<string>>();

        var pgEx = new PostgresException("duplicate", "ERROR", "ERROR", "23505");
        _uow.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns<Task>(_ => Task.FromException(new DbUpdateException("save failed", pgEx)));

        var sut = new TransactionBehavior<StandardCommandT, ServiceResult<string>>(_uow, _audit);

        var result = await sut.Handle(
            new StandardCommandT(),
            _ => Task.FromResult(ServiceResult<string>.Success("v")),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.UniqueViolation);
        result.Error.Type.ShouldBe(ErrorType.Conflict);
    }

    public sealed record StandardCommand : IRequest<ServiceResult>;

    public sealed record StandardCommandT : IRequest<ServiceResult<string>>;

    public sealed record TestQuery : IQuery<string>;

    public sealed record BypassCommand : IRequest<ServiceResult>, IBypassTransactionBehavior;

    public sealed record ManualCommand : IRequest<ServiceResult>, IManualTransactionRequest;

    public sealed record MappedUniqueCommand : IRequest<ServiceResult>, IHasUniqueConstraintMapping
    {
        public string? MapUniqueConstraintViolation(string? constraintName) => "نام برند تکراری است.";
    }
}
