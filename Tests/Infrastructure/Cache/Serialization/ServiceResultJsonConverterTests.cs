using Application.Category.Features.Shared;
using Infrastructure.Cache.Serialization;
using Newtonsoft.Json;
using SharedKernel.Models;
using SharedKernel.Results;
using Shouldly;

namespace Tests.Infrastructure.Cache.Serialization;

public class ServiceResultJsonConverterTests
{
    private static readonly JsonConverter Converter = new ServiceResultJsonConverter();

    private static T RoundTrip<T>(T value)
    {
        var json = JsonConvert.SerializeObject(value, Converter);
        return JsonConvert.DeserializeObject<T>(json, Converter)!;
    }

    [Fact]
    public void RoundTrip_SuccessWithPaginatedResult_PreservesItemsAndPaging()
    {
        var item = new CategoryDto { Id = Guid.NewGuid(), Name = "Brakes", Slug = "brakes", SortOrder = 3 };
        var original = ServiceResult<PaginatedResult<CategoryDto>>.Success(
            PaginatedResult<CategoryDto>.Create([item], totalCount: 25, page: 2, pageSize: 10));

        var restored = RoundTrip(original);

        restored.IsSuccess.ShouldBeTrue();
        restored.Value.TotalCount.ShouldBe(25);
        restored.Value.Page.ShouldBe(2);
        restored.Value.PageSize.ShouldBe(10);
        restored.Value.Items.Single().ShouldBe(item);
    }

    [Fact]
    public void RoundTrip_SuccessWithNullValue_StaysSuccessfulWithNull()
    {
        var restored = RoundTrip(ServiceResult<CategoryDto?>.Success(null));

        restored.IsSuccess.ShouldBeTrue();
        restored.ValueOrDefault.ShouldBeNull();
    }

    [Fact]
    public void RoundTrip_Failure_PreservesError()
    {
        var restored = RoundTrip(ServiceResult<CategoryDto>.NotFound("missing"));

        restored.IsFailure.ShouldBeTrue();
        restored.Error.Type.ShouldBe(ErrorType.NotFound);
        restored.Error.Message.ShouldBe("missing");
    }

    [Fact]
    public void CanConvert_OnlyServiceResultOfT()
    {
        Converter.CanConvert(typeof(ServiceResult<int>)).ShouldBeTrue();
        Converter.CanConvert(typeof(ServiceResult)).ShouldBeFalse();
        Converter.CanConvert(typeof(PaginatedResult<int>)).ShouldBeFalse();
    }
}
