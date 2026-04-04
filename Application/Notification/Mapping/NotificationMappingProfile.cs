namespace Application.Notification.Mapping;

public class NotificationMappingProfile : Profile
{
    public NotificationMappingProfile()
    {
        CreateMap<Domain.Notification.Aggregates.Notification, NotificationDto>();
    }
}