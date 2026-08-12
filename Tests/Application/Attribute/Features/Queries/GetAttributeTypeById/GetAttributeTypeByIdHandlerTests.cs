using Application.Attribute.Features.Queries.GetAttributeTypeById;
using Application.Attribute.Features.Shared;
using Domain.Attribute.Aggregates;
using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Attribute.Features.Queries.GetAttributeTypeById;

public class GetAttributeTypeByIdHandlerTests
{
    private readonly IAttributeRepository _repository = Substitute.For<IAttributeRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly GetAttributeTypeByIdHandler _sut;

    public GetAttributeTypeByIdHandlerTests()
    {
        _sut = new GetAttributeTypeByIdHandler(_repository, _mapper);
    }

    [Fact]
    public async Task Handle_WhenTypeNotFound_ReturnsNotFoundServiceResult()
    {
        var query = new GetAttributeTypeByIdQuery(Guid.NewGuid());

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _mapper.DidNotReceiveWithAnyArgs().Map<AttributeTypeDto>(default!);
    }

    [Fact]
    public async Task Handle_WhenTypeExists_ReturnsMappedDtoAsSuccess()
    {
        var id = Guid.NewGuid();
        var query = new GetAttributeTypeByIdQuery(id);

        var type = await new AttributeTypeBuilder().WithName("color").BuildAsync();
        var expectedDto = new AttributeTypeDto
        {
            Id = id,
            Name = "color",
            DisplayName = "Color"
        };

        _repository
            .GetAttributeTypeWithValuesAsync(Arg.Any<AttributeTypeId>(), Arg.Any<CancellationToken>())
            .Returns(type);

        _mapper.Map<AttributeTypeDto>(Arg.Any<object>()).Returns(expectedDto);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);
    }

    [Fact]
    public async Task Handle_PassesAttributeTypeIdBuiltFromRequestIdToRepository()
    {
        var id = Guid.NewGuid();
        var query = new GetAttributeTypeByIdQuery(id);
        AttributeTypeId? captured = null;

        _repository
            .GetAttributeTypeWithValuesAsync(
                Arg.Do<AttributeTypeId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((AttributeType?)null);

        _ = await _sut.Handle(query, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
