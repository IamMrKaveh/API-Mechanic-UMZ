using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetPaymentMethod;
using Application.Payment.Features.Shared;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Queries.GetPaymentMethod;

public class GetPaymentMethodHandlerTests
{
    private readonly IPaymentMethodQueryService _queryService = Substitute.For<IPaymentMethodQueryService>(); private readonly GetPaymentMethodHandler _sut;

    public GetPaymentMethodHandlerTests()
    {
        _sut = new GetPaymentMethodHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ReturnsNotFound()
    {
        _queryService
            .GetByIdAsync(Arg.Any<PaymentMethodId>(), Arg.Any<CancellationToken>())
            .Returns((PaymentMethodDto?)null);

        var result = await _sut.Handle(new GetPaymentMethodQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodExists_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        var dto = new PaymentMethodDto { Id = id, Name = "Zarinpal", Code = "zarinpal" };

        _queryService
            .GetByIdAsync(Arg.Is<PaymentMethodId>(x => x == PaymentMethodId.From(id)), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(new GetPaymentMethodQuery(id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }
}
