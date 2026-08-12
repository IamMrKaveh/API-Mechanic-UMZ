using Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using ValidationException = FluentValidation.ValidationException;

namespace Tests.Application.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNextAndReturnsResponse()
    {
        var sut = new ValidationBehavior<TestRequest, ServiceResult>([]);

        var invoked = false;

        var result = await sut.Handle(
            new TestRequest("v"),
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
    public async Task Handle_WhenAllValidatorsPass_CallsNextAndReturnsResponse()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult()));

        var sut = new ValidationBehavior<TestRequest, ServiceResult>(new[] { validator });

        var result = await sut.Handle(
            new TestRequest("v"),
            _ => Task.FromResult(ServiceResult.Success()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await validator.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<TestRequest>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidatorReportsFailures_ThrowsValidationException()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        var failures = new List<ValidationFailure>
    {
        new("Name", "required"),
        new("Age", "must be positive")
    };
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(failures)));

        var sut = new ValidationBehavior<TestRequest, ServiceResult>(new[] { validator });

        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await sut.Handle(
                new TestRequest("v"),
                _ => Task.FromResult(ServiceResult.Success()),
                CancellationToken.None));

        ex.Errors.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenMultipleValidatorsAndAnyFails_AggregatesFailures()
    {
        var pass = Substitute.For<IValidator<TestRequest>>();
        pass.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult()));

        var fail = Substitute.For<IValidator<TestRequest>>();
        fail.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ValidationResult(new[] { new ValidationFailure("X", "bad") })));

        var sut = new ValidationBehavior<TestRequest, ServiceResult>(new[] { pass, fail });

        var ex = await Should.ThrowAsync<ValidationException>(async () =>
            await sut.Handle(
                new TestRequest("v"),
                _ => Task.FromResult(ServiceResult.Success()),
                CancellationToken.None));

        ex.Errors.Count().ShouldBe(1);
        ex.Errors.Single().PropertyName.ShouldBe("X");
    }

    [Fact]
    public async Task Handle_WhenValidatorsReturnEmptyResults_CallsNext()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromResult(new ValidationResult(Array.Empty<ValidationFailure>())));

        var sut = new ValidationBehavior<TestRequest, ServiceResult>([validator]);

        var invoked = false;

        var result = await sut.Handle(
            new TestRequest("v"),
            _ =>
            {
                invoked = true;
                return Task.FromResult(ServiceResult.Success());
            },
            CancellationToken.None);

        invoked.ShouldBeTrue();
        result.ShouldBeSuccess();
    }

    public sealed record TestRequest(string Value) : IRequest<ServiceResult>;
}
