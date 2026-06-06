using Application.User.Features.Commands.UpdateProfile;
using Mapster;
using Presentation.User.Requests;

namespace Presentation.User.Mapping;

public sealed class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateProfileRequest, UpdateProfileCommand>();
    }
}