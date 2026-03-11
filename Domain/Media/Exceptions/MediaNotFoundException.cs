namespace Domain.Media.Exceptions;

public class MediaNotFoundException : DomainException
{
    public MediaId MediaId { get; }

    public MediaNotFoundException(MediaId mediaId)
        : base($"رسانه با شناسه {mediaId.Value} یافت نشد.")
    {
        MediaId = mediaId;
    }

    public MediaNotFoundException(int mediaId)
        : base($"رسانه با شناسه {mediaId} یافت نشد.")
    {
        MediaId = ValueObjects.MediaId.From(Guid.Empty);
    }
}