using Application.Audit.Contracts;
using Application.Common.Behaviors;
using Application.Common.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Common.Behaviors;

public class AuditingBehaviorTests : HandlerTestBase
{
    private readonly IAuditContextEnricher _enricher = Substitute.For<IAuditContextEnricher>();

    private readonly ILogger<AuditingBehavior<TestCommand, ServiceResult>> _logger =
        Substitute.For<ILogger<AuditingBehavior<TestCommand, ServiceResult>>>();

    private readonly AuditingBehavior<TestCommand, ServiceResult> _sut;

    public AuditingBehaviorTests()
    {
        CurrentUserService.IpAddress.Returns("10.0.0.1");
        CurrentUserService.UserAgent.Returns("xunit-runner");
        CurrentUserService.UserId.Returns((Guid?)null);
        CurrentUserService.SessionId.Returns((Guid?)null);
        CurrentUserService.IsAdmin.Returns(false);

        _enricher.Snapshot().Returns(new Dictionary<string, string>());

        _sut = new AuditingBehavior<TestCommand, ServiceResult>(
            AuditService, CurrentUserService, _enricher, _logger);
    }

    [Fact]
    public async Task Handle_WhenNonAuditableRequest_SkipsAuditing()
    {
        var behavior = new AuditingBehavior<NonAuditableRequest, ServiceResult>(
            AuditService,
            CurrentUserService,
            _enricher,
            Substitute.For<ILogger<AuditingBehavior<NonAuditableRequest, ServiceResult>>>());

        var result = await behavior.Handle(
            new NonAuditableRequest(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await AuditService.DidNotReceiveWithAnyArgs().LogAsync(
            default!, default!, default!, default, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenSuccess_LogsSuccessActionOnce()
    {
        var command = new TestCommand("Security", "Login");

        var result = await _sut.Handle(
            command,
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await AuditService.Received(1).LogAsync(
            "Security",
            "Login",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Is<string>(s => s!.Contains("executed successfully")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFailure_LogsWithFailedSuffix()
    {
        var command = new TestCommand("Security", "Login");
        var failure = ServiceResult.Failure(Error.Validation("bad input"));

        var result = await _sut.Handle(
            command,
            _ => Task.FromResult(failure),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        await AuditService.Received(1).LogAsync(
            "Security",
            "Login.Failed",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Is<string>(s => s!.Contains("failed") && s.Contains("bad input")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_LogsExceptionActionAndRethrows()
    {
        var command = new TestCommand("Order", "Create");

        async Task<ServiceResult> act() => await _sut.Handle(
            command,
            _ => throw new InvalidOperationException("boom"),
            CancellationToken.None);

        var ex = await Should.ThrowAsync<InvalidOperationException>((Func<Task<ServiceResult>>)act);
        ex.Message.ShouldBe("boom");

        await AuditService.Received(1).LogAsync(
            "Order",
            "Create.Exception",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Is<string>(s => s!.Contains("InvalidOperationException") && s.Contains("boom")),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuditingItselfThrows_DoesNotBreakOuterFlowOnSuccessPath()
    {
        AuditService
            .When(x => x.LogAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId?>(), Arg.Any<string?>(), Arg.Any<string?>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new Exception("audit-sink down"));

        var result = await _sut.Handle(
            new TestCommand("Security", "Login"),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_ClearsEnricherInFinally()
    {
        await _sut.Handle(
            new TestCommand("Security", "Login"),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        _enricher.Received(1).Clear();
    }

    public sealed record TestCommand(string EventType, string Action)
        : IRequest<ServiceResult>, IAuditableCommand
    {
        public string AuditEventType => EventType;
        public string AuditAction => Action;
        public string? AuditEntityType => null;
        public string? AuditEntityId => null;
    }

    public sealed record NonAuditableRequest : IRequest<ServiceResult>;
}
