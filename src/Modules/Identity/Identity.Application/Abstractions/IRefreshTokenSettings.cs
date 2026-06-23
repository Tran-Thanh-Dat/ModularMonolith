namespace Identity.Application.Abstractions;

public interface IRefreshTokenSettings
{
    int ExpirationDays { get; }
}
