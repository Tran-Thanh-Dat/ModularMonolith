using BuildingBlocks.Application.CQRS;

namespace Identity.Application.Auth.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : ICommand<RefreshTokenResponse>;
