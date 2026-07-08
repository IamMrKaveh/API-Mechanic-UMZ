namespace Presentation.Wallet.Requests;

public sealed record CompleteTopUpRequest(string? Authority, string? Status);