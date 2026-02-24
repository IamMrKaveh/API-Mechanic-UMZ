namespace Domain.Media.Exceptions;

public class MediaNotFoundException : DomainException
{
    public int MediaId { get; }

    public MediaNotFoundException(int mediaId)
        : base($"رسانه با شناسه {mediaId} یافت نشد.")
    {
        MediaId = mediaId;
    }
}