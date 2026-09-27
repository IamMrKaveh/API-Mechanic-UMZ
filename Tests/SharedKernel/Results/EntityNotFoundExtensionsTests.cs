using SharedKernel.Results;

namespace Tests.SharedKernel.Results;

public class EntityNotFoundExtensionsTests
{
    private sealed class Widget
    {
        public bool IsDeleted { get; init; }
    }

    [Fact]
    public void ToResultOrNotFound_OnNull_ReturnsNotFound()
    {
        Widget? entity = null;

        var result = entity.ToResultOrNotFound("یافت نشد.");

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void ToResultOrNotFound_OnValue_ReturnsSuccessWithSameInstance()
    {
        var entity = new Widget();

        var result = ((Widget?)entity).ToResultOrNotFound("یافت نشد.");

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(entity);
    }

    [Fact]
    public void ToResultOrNotFound_WithPredicate_TreatsMatchingValueAsNotFound()
    {
        var entity = new Widget { IsDeleted = true };

        var result = ((Widget?)entity).ToResultOrNotFound(x => x.IsDeleted, "یافت نشد.");

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void ToResultOrNotFound_WithPredicate_KeepsNonMatchingValue()
    {
        var entity = new Widget { IsDeleted = false };

        var result = ((Widget?)entity).ToResultOrNotFound(x => x.IsDeleted, "یافت نشد.");

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(entity);
    }

    [Fact]
    public async Task OrNotFoundAsync_OnNullTask_ReturnsNotFound()
    {
        var result = await Task.FromResult<Widget?>(null).OrNotFoundAsync("یافت نشد.");

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task OrNotFoundAsync_OnValueTask_ReturnsSuccess()
    {
        var entity = new Widget();

        var result = await Task.FromResult<Widget?>(entity).OrNotFoundAsync("یافت نشد.");

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(entity);
    }

    [Fact]
    public async Task OrNotFoundAsync_WithPredicate_TreatsMatchingValueAsNotFound()
    {
        var entity = new Widget { IsDeleted = true };

        var result = await Task.FromResult<Widget?>(entity).OrNotFoundAsync(x => x.IsDeleted, "یافت نشد.");

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public void ToServiceResult_OnFailure_ProjectsSameError()
    {
        var failure = ServiceResult<Widget>.NotFound("یافت نشد.");

        var result = failure.ToServiceResult();

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ErrorCode.NotFound);
    }

    [Fact]
    public void ToServiceResult_OnSuccess_ReturnsSuccess()
    {
        var success = ServiceResult<Widget>.Success(new Widget());

        var result = success.ToServiceResult();

        result.IsSuccess.ShouldBeTrue();
    }
}
