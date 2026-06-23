using BuildingBlocks.Application.Abstractions;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddDbContext<NotificationsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", NotificationsConstants.SchemaName))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<NotificationsUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<NotificationsUnitOfWork>());

        services.AddScoped<IEmailSender>(provider =>
        {
            var options = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>()
                          ?? new EmailOptions();

            return options.Provider.Equals("Smtp", StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<SmtpEmailSender>()
                : throw new InvalidOperationException(
                    $"Email provider '{options.Provider}' is not supported.");
        });

        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<INotificationAuthorizationService, NotificationAuthorizationService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailRetryService, EmailRetryService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}
