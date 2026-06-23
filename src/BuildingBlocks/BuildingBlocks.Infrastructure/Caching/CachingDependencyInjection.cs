using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Caching;

public static class CachingDependencyInjection
{
    public static IServiceCollection AddCachingServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddSingleton<ICacheKeyRegistry, CacheKeyRegistry>();
        services.AddScoped<CacheOperationBuffer>();
        services.AddScoped<ICacheOperationBuffer>(static sp => sp.GetRequiredService<CacheOperationBuffer>());
        services.AddScoped<ICacheInvalidationBuffer>(static sp => sp.GetRequiredService<CacheOperationBuffer>());
        services.AddScoped<IPostCommitHook, CacheInvalidationPostCommitHook>();

        var options = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        var provider = options.Provider?.Trim() ?? CacheProviders.Memory;
        var isDevelopment = environment?.IsDevelopment() ?? false;
        var isProduction = environment?.IsProduction() ?? false;
        var normalizedProvider = provider.ToUpperInvariant();

        switch (normalizedProvider)
        {
            case var p when p == CacheProviders.None.ToUpperInvariant():
                services.AddSingleton<ICacheService, NoCacheService>();
                LogProviderSelection(services, CacheProviders.None, CacheProviders.None, isProduction);
                break;

            case var p when p == CacheProviders.Redis.ToUpperInvariant():
                RegisterRedisCache(services, options, isDevelopment, isProduction);
                break;

            case var p when p == CacheProviders.Memory.ToUpperInvariant():
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, MemoryCacheService>();
                LogProviderSelection(services, CacheProviders.Memory, CacheProviders.Memory, isProduction);
                break;

            default:
                throw new InvalidOperationException(
                    $"Cache provider '{options.Provider}' is not supported. " +
                    $"Supported values: {CacheProviders.None}, {CacheProviders.Memory}, {CacheProviders.Redis}. " +
                    $"ErrorCode={CacheErrors.ProviderNotConfigured}");
        }

        return services;
    }

    private static void RegisterRedisCache(
        IServiceCollection services,
        CacheOptions options,
        bool isDevelopment,
        bool isProduction)
    {
        var connectionString = options.Redis.ConnectionString;

        try
        {
            services.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.Configuration = connectionString;
                redisOptions.InstanceName = options.Redis.InstanceName;
            });

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(connectionString));

            services.AddSingleton<ICacheService, RedisCacheService>();
            LogProviderSelection(services, CacheProviders.Redis, CacheProviders.Redis, isProduction);
        }
        catch (Exception ex) when (isDevelopment && options.FallbackToMemoryInDevelopment)
        {
            RegisterMemoryFallback(
                services,
                requestedProvider: CacheProviders.Redis,
                isProduction: false,
                isUnsafeProductionFallback: false,
                fallbackReason: ex);
        }
        catch (Exception ex) when (isProduction && options.FailFastOnRedisUnavailableInProduction)
        {
            throw new InvalidOperationException(
                "Redis cache provider is configured but the connection could not be established. " +
                "Fix Redis connectivity or set Cache:FailFastOnRedisUnavailableInProduction=false to allow fallback. " +
                $"ErrorCode={CacheErrors.ConnectionFailed}",
                ex);
        }
        catch (Exception ex)
        {
            RegisterMemoryFallback(
                services,
                requestedProvider: CacheProviders.Redis,
                isProduction: isProduction,
                isUnsafeProductionFallback: isProduction,
                fallbackReason: ex);
        }
    }

    private static void RegisterMemoryFallback(
        IServiceCollection services,
        string requestedProvider,
        bool isProduction,
        bool isUnsafeProductionFallback,
        Exception fallbackReason)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        LogProviderSelection(
            services,
            requestedProvider,
            CacheProviders.Memory,
            isProduction,
            fallbackReason,
            isUnsafeProductionFallback);
    }

    private static void LogProviderSelection(
        IServiceCollection services,
        string requestedProvider,
        string activeProvider,
        bool isProduction,
        Exception? fallbackReason = null,
        bool isUnsafeProductionFallback = false)
    {
        services.AddSingleton(new CacheProviderSelection(
            requestedProvider,
            activeProvider,
            isProduction,
            isUnsafeProductionFallback,
            fallbackReason));
        services.AddHostedService<CacheStartupLogger>();
    }

    private sealed record CacheProviderSelection(
        string RequestedProvider,
        string ActiveProvider,
        bool IsProduction,
        bool IsUnsafeProductionFallback,
        Exception? FallbackReason);

    private sealed class CacheStartupLogger : IHostedService
    {
        private readonly ILogger<CacheStartupLogger> _logger;
        private readonly CacheProviderSelection _selection;
        private readonly CacheOptions _options;

        public CacheStartupLogger(
            ILogger<CacheStartupLogger> logger,
            CacheProviderSelection selection,
            Microsoft.Extensions.Options.IOptions<CacheOptions> options)
        {
            _logger = logger;
            _selection = selection;
            _options = options.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_selection.FallbackReason is not null)
            {
                if (_selection.IsUnsafeProductionFallback)
                {
                    _logger.LogCritical(
                        _selection.FallbackReason,
                        "Cache provider fallback: requested {RequestedCacheProvider} but using in-memory cache in Production. " +
                        "In-memory cache is process-local and unsafe for multi-instance deployments. " +
                        "Configure Redis connectivity or set Cache:FailFastOnRedisUnavailableInProduction=true.",
                        _selection.RequestedProvider);
                }
                else
                {
                    _logger.LogWarning(
                        _selection.FallbackReason,
                        "Cache provider fallback: requested {RequestedCacheProvider} but using in-memory cache in Development.",
                        _selection.RequestedProvider);
                }
            }

            _logger.LogInformation(
                "Cache provider active: {CacheProvider} (requested {RequestedCacheProvider}). KeyPrefix={KeyPrefix}, DefaultExpirationMinutes={DefaultExpirationMinutes}",
                _selection.ActiveProvider,
                _selection.RequestedProvider,
                _options.KeyPrefix,
                _options.DefaultExpirationMinutes);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
