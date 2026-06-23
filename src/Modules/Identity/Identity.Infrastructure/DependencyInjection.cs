using System.Text;
using BuildingBlocks.Application.Abstractions;
using Identity.Application.Abstractions;
using Identity.Infrastructure.Authentication;
using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Seeding;
using Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<RefreshTokenOptions>(configuration.GetSection(RefreshTokenOptions.SectionName));
        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));

        ValidateJwtOptions(configuration);
        ValidateRefreshTokenOptions(configuration);

        services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<IIdentityUserRepository, IdentityUserRepository>();
        services.AddScoped<IIdentityRefreshTokenRepository, IdentityRefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        // Register concrete UoW for typed injection in Identity/User services.
        // Also register as IUnitOfWork so TransactionBehavior receives it via IEnumerable<IUnitOfWork>.
        services.AddScoped<IdentityUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<IdentityUnitOfWork>());

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenSettings>(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RefreshTokenOptions>>().Value);
        services.AddSingleton<IPasswordResetSettings>(provider =>
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PasswordResetOptions>>().Value);
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IAccountEmailService, AccountEmailService>();
        services.AddScoped<IIdentitySeeder, IdentitySeeder>();

        return services;
    }

    public static IServiceCollection AddIdentityAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                         ?? throw new InvalidOperationException("Jwt configuration is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = environment.IsProduction();
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }

    private static void ValidateJwtOptions(IConfiguration configuration)
    {
        var secret = configuration[$"{JwtOptions.SectionName}:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must be at least 32 characters.");
        }
    }

    private static void ValidateRefreshTokenOptions(IConfiguration configuration)
    {
        var secret = configuration[$"{RefreshTokenOptions.SectionName}:Secret"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("RefreshToken:Secret must be at least 32 characters.");
        }
    }
}
