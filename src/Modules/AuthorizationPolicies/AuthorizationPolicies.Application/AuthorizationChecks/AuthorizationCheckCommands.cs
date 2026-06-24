using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.AuthorizationChecks;
using AuthorizationPolicies.Application.Models;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuthorizationPolicies.Application.AuthorizationChecks;

public sealed record EvaluateAuthorizationCommand(
    Guid UserId,
    string? PermissionCode,
    AuthorizationResourceContextDto ResourceContext,
    string? Action,
    string? ResourceType,
    string? ModuleCode) : ICommand<AuthorizationDecisionResponse>;

public sealed class EvaluateAuthorizationCommandValidator : AbstractValidator<EvaluateAuthorizationCommand>
{
    public EvaluateAuthorizationCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ResourceContext).NotNull();
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.PermissionCode) ||
                       (!string.IsNullOrWhiteSpace(c.Action) && !string.IsNullOrWhiteSpace(c.ResourceType)))
            .WithMessage("PermissionCode or Action+ResourceType is required.")
            .WithErrorCode(AuthorizationCheckErrors.MissingPermissionOrAction);
        RuleFor(c => c.Action)
            .NotEmpty()
            .When(c => !string.IsNullOrWhiteSpace(c.ResourceType));
        RuleFor(c => c.ResourceType)
            .NotEmpty()
            .When(c => !string.IsNullOrWhiteSpace(c.Action));
        RuleFor(c => c.Action).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.Action));
        RuleFor(c => c.ResourceType).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.ResourceType));
        RuleFor(c => c.PermissionCode).MaximumLength(150).When(c => !string.IsNullOrWhiteSpace(c.PermissionCode));
        RuleFor(c => c.ModuleCode).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.ModuleCode));
    }
}

public sealed class EvaluateAuthorizationCommandHandler : IRequestHandler<EvaluateAuthorizationCommand, Result<AuthorizationDecisionResponse>>
{
    private readonly IAuthorizationMatrixService _service;
    private readonly IAuthorizationCheckRequestValidator _requestValidator;

    public EvaluateAuthorizationCommandHandler(
        IAuthorizationMatrixService service,
        IAuthorizationCheckRequestValidator requestValidator)
    {
        _service = service;
        _requestValidator = requestValidator;
    }

    public async Task<Result<AuthorizationDecisionResponse>> Handle(EvaluateAuthorizationCommand request, CancellationToken cancellationToken)
    {
        await _requestValidator.ValidateAsync(
            request.PermissionCode,
            request.ResourceContext,
            request.Action,
            request.ResourceType,
            request.ModuleCode,
            cancellationToken);

        var context = MapContext(request.ResourceContext);
        return await _service.CheckAsync(
            request.UserId, request.PermissionCode, context, request.Action, request.ResourceType, request.ModuleCode, cancellationToken);
    }

    internal static AuthorizationResourceContext MapContext(AuthorizationResourceContextDto dto) =>
        new()
        {
            ResourceType = dto.ResourceType,
            ResourceId = dto.ResourceId,
            TenantId = dto.TenantId,
            OrganizationId = dto.OrganizationId,
            WorkspaceId = dto.WorkspaceId,
            OwnerUserId = dto.OwnerUserId,
            AssignedUserIds = dto.AssignedUserIds ?? [],
            CreatedBy = dto.CreatedBy,
            Metadata = dto.Metadata
        };
}

public sealed record ExplainAuthorizationCommand(
    Guid UserId,
    string? PermissionCode,
    AuthorizationResourceContextDto ResourceContext,
    string? Action,
    string? ResourceType,
    string? ModuleCode) : ICommand<AuthorizationExplanationResponse>;

public sealed class ExplainAuthorizationCommandValidator : AbstractValidator<ExplainAuthorizationCommand>
{
    public ExplainAuthorizationCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ResourceContext).NotNull();
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.PermissionCode) ||
                       (!string.IsNullOrWhiteSpace(c.Action) && !string.IsNullOrWhiteSpace(c.ResourceType)))
            .WithMessage("PermissionCode or Action+ResourceType is required.")
            .WithErrorCode(AuthorizationCheckErrors.MissingPermissionOrAction);
        RuleFor(c => c.Action)
            .NotEmpty()
            .When(c => !string.IsNullOrWhiteSpace(c.ResourceType));
        RuleFor(c => c.ResourceType)
            .NotEmpty()
            .When(c => !string.IsNullOrWhiteSpace(c.Action));
        RuleFor(c => c.Action).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.Action));
        RuleFor(c => c.ResourceType).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.ResourceType));
        RuleFor(c => c.PermissionCode).MaximumLength(150).When(c => !string.IsNullOrWhiteSpace(c.PermissionCode));
        RuleFor(c => c.ModuleCode).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.ModuleCode));
    }
}

public sealed class ExplainAuthorizationCommandHandler : IRequestHandler<ExplainAuthorizationCommand, Result<AuthorizationExplanationResponse>>
{
    private readonly IAuthorizationMatrixService _service;
    private readonly IAuthorizationCheckRequestValidator _requestValidator;

    public ExplainAuthorizationCommandHandler(
        IAuthorizationMatrixService service,
        IAuthorizationCheckRequestValidator requestValidator)
    {
        _service = service;
        _requestValidator = requestValidator;
    }

    public async Task<Result<AuthorizationExplanationResponse>> Handle(ExplainAuthorizationCommand request, CancellationToken cancellationToken)
    {
        await _requestValidator.ValidateAsync(
            request.PermissionCode,
            request.ResourceContext,
            request.Action,
            request.ResourceType,
            request.ModuleCode,
            cancellationToken);

        var context = EvaluateAuthorizationCommandHandler.MapContext(request.ResourceContext);
        return await _service.ExplainAsync(
            request.UserId, request.PermissionCode, context, request.Action, request.ResourceType, request.ModuleCode, cancellationToken);
    }
}
