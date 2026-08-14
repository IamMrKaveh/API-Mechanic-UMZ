using Application.User.Features.Commands.CreateUser;
using Application.User.Features.Shared;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.CreateUser;

public class CreateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly IMapper _mapper = Substitute.For<IMapper>(); private readonly CreateUserHandler _sut;

    public CreateUserHandlerTests()
    {
        _sut = new CreateUserHandler(_userRepository, _mapper);
    }

    private static CreateUserCommand BuildCommand(bool isAdmin = false) =>
        new(
            PhoneNumber: "09121234567",
            FirstName: "Ali",
            LastName: "Ahmadi",
            Email: "ali@example.com",
            IsAdmin: isAdmin);

    [Fact]
    public async Task Handle_WhenPhoneNumberAlreadyExists_ReturnsConflictWithoutAdding()
    {
        _userRepository
            .ExistsByPhoneNumberAsync(
                Arg.Any<PhoneNumber>(),
                Arg.Is<UserId?>(x => x == null),
                Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(BuildCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _userRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        _mapper.DidNotReceiveWithAnyArgs().Map<UserProfileDto>(default!);
    }

    [Fact]
    public async Task Handle_WhenPhoneNumberIsUnique_AddsUserAndReturnsMappedDto()
    {
        _userRepository
            .ExistsByPhoneNumberAsync(
                Arg.Any<PhoneNumber>(),
                Arg.Is<UserId?>(x => x == null),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var expectedDto = new UserProfileDto { Id = Guid.NewGuid() };
        _mapper.Map<UserProfileDto>(Arg.Any<Users>()).Returns(expectedDto);

        var result = await _sut.Handle(BuildCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);
        await _userRepository.Received(1).AddAsync(Arg.Any<Users>(), Arg.Any<CancellationToken>());
        _mapper.Received(1).Map<UserProfileDto>(Arg.Any<Users>());
    }
}
