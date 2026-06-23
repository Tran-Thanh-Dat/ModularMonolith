using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailMessages;

namespace Notifications.Application.EmailMessages.GetEmailMessageById;

public sealed record GetEmailMessageByIdQuery(Guid Id) : IQuery<EmailMessageDetailResponse>;

public sealed class GetEmailMessageByIdQueryValidator : AbstractValidator<GetEmailMessageByIdQuery>
{
    public GetEmailMessageByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetEmailMessageByIdQueryHandler
    : IRequestHandler<GetEmailMessageByIdQuery, Result<EmailMessageDetailResponse>>
{
    private readonly IEmailService _emailService;

    public GetEmailMessageByIdQueryHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Result<EmailMessageDetailResponse>> Handle(
        GetEmailMessageByIdQuery request,
        CancellationToken cancellationToken)
    {
        var message = await _emailService.GetEmailMessageByIdAsync(request.Id, cancellationToken);

        if (message is null)
        {
            throw new NotFoundException(
                EmailErrors.MessageNotFound,
                $"Email message with id '{request.Id}' was not found.");
        }

        return Result<EmailMessageDetailResponse>.Success(message);
    }
}
