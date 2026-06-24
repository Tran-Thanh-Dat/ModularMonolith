using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.OrganizationUsers;

namespace Organizations.Application.OrganizationUsers.AssignOrganizationUser;

public sealed record AssignOrganizationUserCommand(
    Guid TenantId,
    Guid OrganizationId,
    Guid UserId,
    bool IsDefault) : ICommand<AssignOrganizationUserResponse>;

public sealed class AssignOrganizationUserCommandValidator : AbstractValidator<AssignOrganizationUserCommand>
{
    public AssignOrganizationUserCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.OrganizationId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class AssignOrganizationUserCommandHandler : IRequestHandler<AssignOrganizationUserCommand, Result<AssignOrganizationUserResponse>>
{
    private readonly IOrganizationUserService _organizationUserService;

    public AssignOrganizationUserCommandHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result<AssignOrganizationUserResponse>> Handle(AssignOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        var id = await _organizationUserService.AssignAsync(
            request.TenantId,
            request.OrganizationId,
            request.UserId,
            request.IsDefault,
            cancellationToken);

        return Result<AssignOrganizationUserResponse>.Success(new AssignOrganizationUserResponse { Id = id });
    }
}
