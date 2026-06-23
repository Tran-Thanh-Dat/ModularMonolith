using BuildingBlocks.Application.CQRS;

namespace Identity.Application.Auth.Login;

public sealed record LoginCommand(
    string UserNameOrEmail,
    string Password,
    bool RememberMe,
    string? IpAddress = null) : ICommand<LoginResponse>;
