using Application.Common.Behaviors;
using FluentValidation.Results;
using MediatR;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using ValidationException = FluentValidation.ValidationException;

namespace Tests.Application.Common.Behaviors;

public class UnhandledExceptionLoggingBehaviorTests
{
    private readonly ILogger<UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>> _logger = Substitute.For<ILogger<UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>>>();

    [Fact]
    public async Task Handle_WhenNoException_ReturnsResponse()
    {
        var sut = new UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>(_logger);

        var result = await sut.Handle(
            new TestRequest(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_RethrowsWithoutWrapping()
    {
        var sut = new UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>(_logger);

        var ex = await Should.ThrowAsync<DomainException>(async () =>
            await sut.Handle(
                new TestRequest(),
                _ => throw new DomainException("rule"),
                CancellationToken.None));

        ex.Message.ShouldBe("rule");
    }

    [Fact]
    public async Task Handle_WhenValidationExceptionThrown_RethrowsWithoutWrapping()
    {
        var sut = new UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>(_logger);
        var failures = new List<ValidationFailure> { new("P", "m") };

        await Should.ThrowAsync<ValidationException>(async () =>
            await sut.Handle(
                new TestRequest(),
                _ => throw new ValidationException(failures),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenOperationCanceledThrown_RethrowsWithoutWrapping()
    {
        var sut = new UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>(_logger);

        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await sut.Handle(
                new TestRequest(),
                _ => throw new OperationCanceledException(),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenGenericExceptionThrown_LogsAndRethrows()
    {
        var sut = new UnhandledExceptionLoggingBehavior<TestRequest, ServiceResult>(_logger);

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.Handle(
                new TestRequest(),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        ex.Message.ShouldBe("boom");
    }

    public sealed record TestRequest : IRequest<ServiceResult>;
}
