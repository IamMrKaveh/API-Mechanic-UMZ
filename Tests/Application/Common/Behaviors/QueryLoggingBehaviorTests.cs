using Application.Common.Behaviors;
using Application.Common.Interfaces;
using MediatR;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Common.Behaviors;

public class QueryLoggingBehaviorTests
{
    [Fact]
    public async Task Handle_WhenRequestIsNotQuery_ForwardsWithoutLogging()
    {
        var logger = Substitute.For<ILogger<QueryLoggingBehavior<NonQueryRequest, ServiceResult>>>(); var sut = new QueryLoggingBehavior<NonQueryRequest, ServiceResult>(logger); var invoked = false;

        var result = await sut.Handle(
            new NonQueryRequest(),
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
    public async Task Handle_WhenRequestIsQuery_InvokesNextAndReturnsResult()
    {
        var logger = Substitute.For<ILogger<QueryLoggingBehavior<TestQuery, ServiceResult<string>>>>();
        var sut = new QueryLoggingBehavior<TestQuery, ServiceResult<string>>(logger);

        var result = await sut.Handle(
            new TestQuery(),
            _ => Task.FromResult(ServiceResult<string>.Success("ok")),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe("ok");
    }

    [Fact]
    public async Task Handle_WhenQueryThrows_Rethrows()
    {
        var logger = Substitute.For<ILogger<QueryLoggingBehavior<TestQuery, ServiceResult<string>>>>();
        var sut = new QueryLoggingBehavior<TestQuery, ServiceResult<string>>(logger);

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.Handle(
                new TestQuery(),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        ex.Message.ShouldBe("boom");
    }

    public sealed record TestQuery : IQuery<string>;

    public sealed record NonQueryRequest : IRequest<ServiceResult>;
}
