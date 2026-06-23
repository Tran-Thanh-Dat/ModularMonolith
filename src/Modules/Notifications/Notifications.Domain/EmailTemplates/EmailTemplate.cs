using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Notifications.Domain.EmailTemplates;

public sealed class EmailTemplate : SoftDeletableEntity
{
    private EmailTemplate()
    {
    }

    private EmailTemplate(
        Guid id,
        string code,
        string name,
        string subject,
        string body,
        bool isHtml,
        string? description,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        Code = code;
        Name = name;
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
        Description = description;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string Subject { get; private set; } = default!;

    public string Body { get; private set; } = default!;

    public bool IsHtml { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public static EmailTemplate Create(
        string code,
        string name,
        string subject,
        string body,
        bool isHtml,
        string? description,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Template code is required.", "Email.TemplateCodeAlreadyExists");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Template name is required.", "Email.SendFailed");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("Template subject is required.", "Email.SendFailed");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Template body is required.", "Email.SendFailed");
        }

        return new EmailTemplate(
            Guid.NewGuid(),
            code.Trim(),
            name.Trim(),
            subject.Trim(),
            body,
            isHtml,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            createdAt,
            createdBy);
    }

    public void Update(string name, string subject, string body, bool isHtml, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Template name is required.", "Email.SendFailed");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("Template subject is required.", "Email.SendFailed");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Template body is required.", "Email.SendFailed");
        }

        Name = name.Trim();
        Subject = subject.Trim();
        Body = body;
        IsHtml = isHtml;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Email template is already deleted.", "Email.TemplateNotFound");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
