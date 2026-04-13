using Application.User.Features.Commands.CreateUserAddress;
using Application.User.Features.Commands.UpdateProfile;
using Application.User.Features.Commands.UpdateUserAddress;
using Application.User.Features.Shared;
using Domain.Security.Aggregates;
using Domain.User.Entities;
using Mapster;

namespace Application.User.Mapping;

public class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.User.Aggregates.User, UserProfileDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber != null ? src.PhoneNumber.Value : string.Empty)
            .Map(dest => dest.FirstName, src => src.FullName.FirstName)
            .Map(dest => dest.LastName, src => src.FullName.LastName)
            .Map(dest => dest.Email, src => src.Email.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsAdmin, src => src.IsAdmin)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastLoginAt, src => src.LastLoginAt)
            .Map(dest => dest.UserAddresses, src => src.Addresses.Adapt<List<UserAddressDto>>());

        config.NewConfig<UserAddress, UserAddressDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.ReceiverName, src => src.ReceiverName)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber.Value)
            .Map(dest => dest.Province, src => src.Province)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.PostalCode, src => src.PostalCode)
            .Map(dest => dest.Latitude, src => src.Latitude)
            .Map(dest => dest.Longitude, src => src.Longitude)
            .Map(dest => dest.IsDefault, src => src.IsDefault);

        config.NewConfig<UserSession, UserSessionDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CreatedByIp, src => src.IpAddress.Value)
            .Map(dest => dest.DeviceInfo, src => src.DeviceInfo.Value)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.LastActivityAt, src => src.LastActivityAt)
            .Map(dest => dest.ExpiresAt, src => src.ExpiresAt)
            .Ignore(dest => dest.SessionType)
            .Ignore(dest => dest.BrowserInfo)
            .Ignore(dest => dest.IsCurrent);

        config.NewConfig<UpdateProfileDto, UpdateProfileCommand>()
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .IgnoreNonMapped(true);

        config.NewConfig<AddUserAddressDto, CreateUserAddressCommand>()
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.ReceiverName, src => src.ReceiverName)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.Province, src => src.Province)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.PostalCode, src => src.PostalCode)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.Latitude, src => src.Latitude)
            .Map(dest => dest.Longitude, src => src.Longitude)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateUserAddressDto, UpdateUserAddressCommand>()
            .Map(dest => dest.Title, src => src.Title)
            .Map(dest => dest.ReceiverName, src => src.ReceiverName)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.Province, src => src.Province)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.PostalCode, src => src.PostalCode)
            .Map(dest => dest.IsDefault, src => src.IsDefault)
            .Map(dest => dest.Latitude, src => src.Latitude)
            .Map(dest => dest.Longitude, src => src.Longitude)
            .IgnoreNonMapped(true);
    }
}