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
RUN chmod +x /usr/local/bin/docker-entrypoint.sh

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=90s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["/usr/local/bin/docker-entrypoint.sh"]
