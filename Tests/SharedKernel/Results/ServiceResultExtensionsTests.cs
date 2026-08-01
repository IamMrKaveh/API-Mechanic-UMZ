using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.SharedKernel.Results;

public class ServiceResultExtensionsTests
{
    [Fact]
    public void ThrowIfFailure_OnSuccess_ReturnsSameResultWithoutThrowing()
    {
        var input = ServiceResult.Success();

        var output = input.ThrowIfFailure();

        output.ShouldBeSameAs(input);
    }

    [Fact]
    public void ThrowIfFailure_OnFailure_ThrowsInvalidOperationWithCodeAndMessage()
    {
        var sut = ServiceResult.Failure(Error.Conflict("dup"));

        var ex = Should.Throw<InvalidOperationException>(() => sut.ThrowIfFailure());

        ex.Message.ShouldContain(ErrorCode.Conflict);
        ex.Message.ShouldContain("dup");
    }

    [Fact]
    public void ThrowIfFailureGeneric_OnSuccess_ReturnsSameResultWithoutThrowing()
    {
        var input = ServiceResult<int>.Success(1);

        var output = input.ThrowIfFailure();

        output.ShouldBeSameAs(input);
    }

    [Fact]
    public void ThrowIfFailureGeneric_OnFailure_ThrowsInvalidOperationWithCodeAndMessage()
    {
        var sut = ServiceResult<int>.Failure(Error.NotFound("nf"));

        var ex = Should.Throw<InvalidOperationException>(() => sut.ThrowIfFailure());

        ex.Message.ShouldContain(ErrorCode.NotFound);
        ex.Message.ShouldContain("nf");
    }

    [Fact]
    public void LogIfFailure_OnSuccess_DoesNotInvokeLogger()
    {
        var logger = Substitute.For<ILogger>();

        ServiceResult.Success().LogIfFailure(logger);

        logger.DidNotReceiveWithAnyArgs().Log(
            default, default, Arg.Any<object>(), default, Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public void LogIfFailure_OnFailure_InvokesLoggerAtWarningLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        ServiceResult.Failure(Error.Conflict("dup")).LogIfFailure(logger);

        logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Warning, default, Arg.Any<object>(), default, Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public void LogIfFailureGeneric_OnFailure_InvokesLoggerAtWarningLevel()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        ServiceResult<int>.Failure(Error.Conflict("dup")).LogIfFailure(logger);

        logger.ReceivedWithAnyArgs(1).Log(
            LogLevel.Warning, default, Arg.Any<object>(), default, Arg.Any<Func<object, Exception?, string>>()!);
    }

    [Fact]
    public async Task MapAsync_OnSuccess_TransformsValueAndKeepsSuccess()
    {
        var input = Task.FromResult(ServiceResult<int>.Success(4));

        var output = await input.MapAsync(x => x * 3);

        output.ShouldBeSuccess();
        output.Value.ShouldBe(12);
    }

    [Fact]
    public async Task MapAsync_OnFailure_PropagatesFailureWithoutInvokingMapper()
    {
        var mapperCalled = false;
        var input = Task.FromResult(ServiceResult<int>.Failure(Error.NotFound("nf")));

        var output = await input.MapAsync(x => { mapperCalled = true; return x * 3; });

        mapperCalled.ShouldBeFalse();
        output.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task BindAsync_OnSuccess_InvokesBinderWithValue()
    {
        var input = Task.FromResult(ServiceResult<int>.Success(5));

        var output = await input.BindAsync(x => Task.FromResult(ServiceResult<string>.Success($"v{x}")));

        output.ShouldBeSuccess();
        output.Value.ShouldBe("v5");
    }

    [Fact]
    public async Task BindAsync_OnFailure_PropagatesFailureWithoutInvokingBinder()
    {
        var binderCalled = false;
        var input = Task.FromResult(ServiceResult<int>.Failure(Error.NotFound("nf")));

        var output = await input.BindAsync(x =>
        {
            binderCalled = true;
            return Task.FromResult(ServiceResult<string>.Success("y"));
        });

        binderCalled.ShouldBeFalse();
        output.ShouldFailWith(ErrorCode.NotFound);
    }
}
