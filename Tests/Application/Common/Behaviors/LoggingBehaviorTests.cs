using Application.Common.Behaviors;
using Application.Common.Interfaces;
using FluentValidation.Results;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using ValidationException = FluentValidation.ValidationException;

namespace Tests.Application.Common.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestIsNotCommand_ForwardsWithoutLogging()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<NonCommandRequest, ServiceResult>>>(); var sut = new LoggingBehavior<NonCommandRequest, ServiceResult>(logger); var invoked = false;

        var result = await sut.Handle(
            new NonCommandRequest(),
            _ =>
            {
                invoked = true;
                return Task.FromResult(ServiceResult.Success());
            },
            CancellationToken.None);

        invoked.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenRequestIsCommand_InvokesNextAndReturnsResult()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggableCommand, ServiceResult>>>();
        var sut = new LoggingBehavior<LoggableCommand, ServiceResult>(logger);

        var result = await sut.Handle(
            new LoggableCommand("Ali", "secret"),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenValidationExceptionThrown_Rethrows()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggableCommand, ServiceResult>>>();
        var sut = new LoggingBehavior<LoggableCommand, ServiceResult>(logger);

        var failures = new List<ValidationFailure>
    {
        new("Name", "required") { AttemptedValue = null }
    };

        await Should.ThrowAsync<ValidationException>(async () =>
            await sut.Handle(
                new LoggableCommand("Ali", "secret"),
                _ => throw new ValidationException(failures),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenUnexpectedExceptionThrown_Rethrows()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggableCommand, ServiceResult>>>();
        var sut = new LoggingBehavior<LoggableCommand, ServiceResult>(logger);

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.Handle(
                new LoggableCommand("Ali", "secret"),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        ex.Message.ShouldBe("boom");
    }

    public sealed record LoggableCommand(string Name, string Password) : ICommand;

    public sealed record NonCommandRequest : IRequest<ServiceResult>;
}
