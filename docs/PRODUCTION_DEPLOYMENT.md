# Production Deployment

This guide summarizes production deployment. **Full details:** [../PRODUCTION.md](../PRODUCTION.md).

## Pre-flight checklist

- [ ] Set strong `Jwt:Secret` and `RefreshToken:Secret` (32+ chars)
- [ ] Set real `ConnectionStrings:DefaultConnection`
- [ ] Set `Cache:Provider=Redis` with valid connection string (multi-instance)
- [ ] Disable or protect Hangfire dashboard
- [ ] Disable Swagger (automatic in Production environment)
- [ ] Configure CORS allowed origins
- [ ] Set `AdminSeed` password or disable seed after bootstrap
- [ ] Configure SMTP for real email
- [ ] Review [../SECURITY.md](../SECURITY.md)

## Environment

```bash
ASPNETCORE_ENVIRONMENT=Production
```

`ProductionStartupValidator` runs at startup and fails fast on weak/missing critical config.

## Docker

Production image build and compose patterns: [../DOCKER.md](../DOCKER.md).

## Health probes

Configure orchestrator to use:

- Liveness: `GET /health/live`
- Readiness: `GET /health/ready`

See [MONITORING.md](./MONITORING.md).

## CI/CD

Pipeline build, test, and image publish: [../CI.md](../CI.md).

## Secrets management

Never commit secrets. Use environment variables, secret stores, or mounted files — [../SECRETS.md](../SECRETS.md).

## Observability

- Structured Serilog output
- Correlation ID on all requests
- Protected `/api/v1/monitoring/*` for operators

## Related docs

| Topic | Document |
|-------|----------|
| Security hardening | [../SECURITY.md](../SECURITY.md) |
| Docker | [../DOCKER.md](../DOCKER.md) |
| Troubleshooting | [TROUBLESHOOTING.md](./TROUBLESHOOTING.md) |
| Architecture | [ARCHITECTURE.md](./ARCHITECTURE.md) |
