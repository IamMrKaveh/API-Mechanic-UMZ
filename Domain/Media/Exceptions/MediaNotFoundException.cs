using Domain.Common.Exceptions;
using Domain.Media.ValueObjects;

namespace Domain.Media.Exceptions;

public sealed class MediaNotFoundException : DomainException
{
    public MediaId MediaId { get; }

    public override string ErrorCode => "MEDIA_NOT_FOUND";

    public MediaNotFoundException(MediaId mediaId)
        : base($"رسانه با شناسه {mediaId} یافت نشد.")
    {
        MediaId = mediaId;
    }
}