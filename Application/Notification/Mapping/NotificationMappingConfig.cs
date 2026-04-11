using Application.Notification.Features.Shared;
using Domain.Notification.Aggregates;
using Mapster;

namespace Application.Notification.Mapping;

public class NotificationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Notification, NotificationDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.UserId.Value)
            .Map(dest => dest.IsRead, src => src.IsRead)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}