namespace Application.Media.Features.Commands.SetPrimaryMedia;

public record SetPrimaryMediaCommand(
    Guid MediaId)
    : ICommand;