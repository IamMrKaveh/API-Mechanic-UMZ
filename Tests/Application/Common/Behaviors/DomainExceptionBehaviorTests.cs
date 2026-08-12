using Application.Common.Behaviors;
using MediatR;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Common.Behaviors;

public class DomainExceptionBehaviorTests
{
    private readonly ILogger<DomainExceptionBehavior<TestRequest, ServiceResult>> _logger = Substitute.For<ILogger<DomainExceptionBehavior<TestRequest, ServiceResult>>>();

    private readonly ILogger<DomainExceptionBehavior<TestRequestT, ServiceResult<int>>> _loggerT =
        Substitute.For<ILogger<DomainExceptionBehavior<TestRequestT, ServiceResult<int>>>>();

    [Fact]
    public async Task Handle_WhenDelegateSucceeds_ReturnsResponseAsIs()
    {
        var sut = new DomainExceptionBehavior<TestRequest, ServiceResult>(_logger);

        var result = await sut.Handle(
            new TestRequest(),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ReturnsServiceResultFailureWithBusinessRuleCode()
    {
        var sut = new DomainExceptionBehavior<TestRequest, ServiceResult>(_logger);

        var result = await sut.Handle(
            new TestRequest(),
            _ => throw new DomainException("rule broken"),
            CancellationToken.None);

        result.ShouldFailWith("Domain.Rule");
        result.Error.Message.ShouldBe("rule broken");
        result.Error.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ForServiceResultT_ReturnsFailureWithBusinessRuleCode()
    {
        var sut = new DomainExceptionBehavior<TestRequestT, ServiceResult<int>>(_loggerT);

        var result = await sut.Handle(
            new TestRequestT(),
            _ => throw new DomainException("rule broken t"),
            CancellationToken.None);

        result.ShouldFailWith("Domain.Rule");
        result.Error.Message.ShouldBe("rule broken t");
        result.Error.Type.ShouldBe(ErrorType.BusinessRule);
    }

    [Fact]
    public async Task Handle_WhenNonDomainExceptionThrown_Rethrows()
    {
        var sut = new DomainExceptionBehavior<TestRequest, ServiceResult>(_logger);

        var ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.Handle(
                new TestRequest(),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        ex.Message.ShouldBe("boom");
    }

    public sealed record TestRequest : IRequest<ServiceResult>;

    public sealed record TestRequestT : IRequest<ServiceResult<int>>;
}
