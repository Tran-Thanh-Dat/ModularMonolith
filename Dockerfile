# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ModularMonolith.sln ./

# Copy project files first for better layer caching, then restore.
COPY src/BuildingBlocks/BuildingBlocks.Domain/BuildingBlocks.Domain.csproj src/BuildingBlocks/BuildingBlocks.Domain/
COPY src/BuildingBlocks/BuildingBlocks.Application/BuildingBlocks.Application.csproj src/BuildingBlocks/BuildingBlocks.Application/
COPY src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/BuildingBlocks.Infrastructure/
COPY src/BuildingBlocks/BuildingBlocks.Web/BuildingBlocks.Web.csproj src/BuildingBlocks/BuildingBlocks.Web/
COPY src/ApiHost/ApiHost.csproj src/ApiHost/
COPY src/Modules/Identity/Identity.Domain/Identity.Domain.csproj src/Modules/Identity/Identity.Domain/
COPY src/Modules/Identity/Identity.Application/Identity.Application.csproj src/Modules/Identity/Identity.Application/
COPY src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj src/Modules/Identity/Identity.Infrastructure/
COPY src/Modules/Identity/Identity.Api/Identity.Api.csproj src/Modules/Identity/Identity.Api/
COPY src/Modules/Users/Users.Domain/Users.Domain.csproj src/Modules/Users/Users.Domain/
COPY src/Modules/Users/Users.Application/Users.Application.csproj src/Modules/Users/Users.Application/
COPY src/Modules/Users/Users.Infrastructure/Users.Infrastructure.csproj src/Modules/Users/Users.Infrastructure/
COPY src/Modules/Users/Users.Api/Users.Api.csproj src/Modules/Users/Users.Api/
COPY src/Modules/AuditLogs/AuditLogs.Domain/AuditLogs.Domain.csproj src/Modules/AuditLogs/AuditLogs.Domain/
COPY src/Modules/AuditLogs/AuditLogs.Application/AuditLogs.Application.csproj src/Modules/AuditLogs/AuditLogs.Application/
COPY src/Modules/AuditLogs/AuditLogs.Infrastructure/AuditLogs.Infrastructure.csproj src/Modules/AuditLogs/AuditLogs.Infrastructure/
COPY src/Modules/AuditLogs/AuditLogs.Api/AuditLogs.Api.csproj src/Modules/AuditLogs/AuditLogs.Api/
COPY src/Modules/Categories/Categories.Domain/Categories.Domain.csproj src/Modules/Categories/Categories.Domain/
COPY src/Modules/Categories/Categories.Application/Categories.Application.csproj src/Modules/Categories/Categories.Application/
COPY src/Modules/Categories/Categories.Infrastructure/Categories.Infrastructure.csproj src/Modules/Categories/Categories.Infrastructure/
COPY src/Modules/Categories/Categories.Api/Categories.Api.csproj src/Modules/Categories/Categories.Api/
COPY src/Modules/Settings/Settings.Domain/Settings.Domain.csproj src/Modules/Settings/Settings.Domain/
COPY src/Modules/Settings/Settings.Application/Settings.Application.csproj src/Modules/Settings/Settings.Application/
COPY src/Modules/Settings/Settings.Infrastructure/Settings.Infrastructure.csproj src/Modules/Settings/Settings.Infrastructure/
COPY src/Modules/Settings/Settings.Api/Settings.Api.csproj src/Modules/Settings/Settings.Api/
COPY src/Modules/Organizations/Organizations.Domain/Organizations.Domain.csproj src/Modules/Organizations/Organizations.Domain/
COPY src/Modules/Organizations/Organizations.Application/Organizations.Application.csproj src/Modules/Organizations/Organizations.Application/
COPY src/Modules/Organizations/Organizations.Infrastructure/Organizations.Infrastructure.csproj src/Modules/Organizations/Organizations.Infrastructure/
COPY src/Modules/Organizations/Organizations.Api/Organizations.Api.csproj src/Modules/Organizations/Organizations.Api/
COPY src/Modules/AuthorizationPolicies/AuthorizationPolicies.Domain/AuthorizationPolicies.Domain.csproj src/Modules/AuthorizationPolicies/AuthorizationPolicies.Domain/
COPY src/Modules/AuthorizationPolicies/AuthorizationPolicies.Application/AuthorizationPolicies.Application.csproj src/Modules/AuthorizationPolicies/AuthorizationPolicies.Application/
COPY src/Modules/AuthorizationPolicies/AuthorizationPolicies.Infrastructure/AuthorizationPolicies.Infrastructure.csproj src/Modules/AuthorizationPolicies/AuthorizationPolicies.Infrastructure/
COPY src/Modules/AuthorizationPolicies/AuthorizationPolicies.Api/AuthorizationPolicies.Api.csproj src/Modules/AuthorizationPolicies/AuthorizationPolicies.Api/
COPY src/Modules/MasterData/MasterData.Domain/MasterData.Domain.csproj src/Modules/MasterData/MasterData.Domain/
COPY src/Modules/MasterData/MasterData.Application/MasterData.Application.csproj src/Modules/MasterData/MasterData.Application/
COPY src/Modules/MasterData/MasterData.Infrastructure/MasterData.Infrastructure.csproj src/Modules/MasterData/MasterData.Infrastructure/
COPY src/Modules/MasterData/MasterData.Api/MasterData.Api.csproj src/Modules/MasterData/MasterData.Api/
COPY src/Modules/AsyncTasks/AsyncTasks.Domain/AsyncTasks.Domain.csproj src/Modules/AsyncTasks/AsyncTasks.Domain/
COPY src/Modules/AsyncTasks/AsyncTasks.Application/AsyncTasks.Application.csproj src/Modules/AsyncTasks/AsyncTasks.Application/
COPY src/Modules/AsyncTasks/AsyncTasks.Infrastructure/AsyncTasks.Infrastructure.csproj src/Modules/AsyncTasks/AsyncTasks.Infrastructure/
COPY src/Modules/AsyncTasks/AsyncTasks.Api/AsyncTasks.Api.csproj src/Modules/AsyncTasks/AsyncTasks.Api/
COPY src/Modules/Files/Files.Domain/Files.Domain.csproj src/Modules/Files/Files.Domain/
COPY src/Modules/Files/Files.Application/Files.Application.csproj src/Modules/Files/Files.Application/
COPY src/Modules/Files/Files.Infrastructure/Files.Infrastructure.csproj src/Modules/Files/Files.Infrastructure/
COPY src/Modules/Files/Files.Api/Files.Api.csproj src/Modules/Files/Files.Api/
COPY src/Modules/Notifications/Notifications.Domain/Notifications.Domain.csproj src/Modules/Notifications/Notifications.Domain/
COPY src/Modules/Notifications/Notifications.Application/Notifications.Application.csproj src/Modules/Notifications/Notifications.Application/
COPY src/Modules/Notifications/Notifications.Infrastructure/Notifications.Infrastructure.csproj src/Modules/Notifications/Notifications.Infrastructure/
COPY src/Modules/Notifications/Notifications.Api/Notifications.Api.csproj src/Modules/Notifications/Notifications.Api/
COPY src/Modules/BackgroundJobs/BackgroundJobs.Domain/BackgroundJobs.Domain.csproj src/Modules/BackgroundJobs/BackgroundJobs.Domain/
COPY src/Modules/BackgroundJobs/BackgroundJobs.Application/BackgroundJobs.Application.csproj src/Modules/BackgroundJobs/BackgroundJobs.Application/
COPY src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure/BackgroundJobs.Infrastructure.csproj src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure/
COPY src/Modules/BackgroundJobs/BackgroundJobs.Api/BackgroundJobs.Api.csproj src/Modules/BackgroundJobs/BackgroundJobs.Api/
COPY src/Modules/Monitoring/Monitoring.Application/Monitoring.Application.csproj src/Modules/Monitoring/Monitoring.Application/
COPY src/Modules/Monitoring/Monitoring.Infrastructure/Monitoring.Infrastructure.csproj src/Modules/Monitoring/Monitoring.Infrastructure/
COPY src/Modules/Monitoring/Monitoring.Api/Monitoring.Api.csproj src/Modules/Monitoring/Monitoring.Api/

RUN dotnet restore src/ApiHost/ApiHost.csproj

COPY src/ src/

RUN dotnet publish src/ApiHost/ApiHost.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Docker \
    DOTNET_EnableDiagnostics=0

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gosu \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/uploads /app/logs

COPY --from=build --chown=app:app /app/publish .

COPY docker/docker-entrypoint.sh /usr/local/bin/docker-entrypoint.sh
RUN sed -i 's/\r$//' /usr/local/bin/docker-entrypoint.sh \
    && chmod +x /usr/local/bin/docker-entrypoint.sh

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
