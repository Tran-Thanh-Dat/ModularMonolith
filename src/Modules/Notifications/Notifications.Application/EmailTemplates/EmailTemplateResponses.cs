namespace Notifications.Application.EmailTemplates;



public sealed class EmailTemplateListItemResponse

{

    public Guid Id { get; init; }



    public string Code { get; init; } = default!;



    public string Name { get; init; } = default!;



    public string Subject { get; init; } = default!;



    public bool IsHtml { get; init; }



    public bool IsActive { get; init; }



    public DateTimeOffset CreatedAt { get; init; }

}



public sealed class EmailTemplateDetailResponse

{

    public Guid Id { get; init; }



    public string Code { get; init; } = default!;



    public string Name { get; init; } = default!;



    public string Subject { get; init; } = default!;



    public string Body { get; init; } = default!;



    public bool IsHtml { get; init; }



    public bool IsActive { get; init; }



    public string? Description { get; init; }



    public DateTimeOffset CreatedAt { get; init; }



    public Guid? CreatedBy { get; init; }



    public DateTimeOffset? UpdatedAt { get; init; }



    public Guid? UpdatedBy { get; init; }

}



public sealed class CreateEmailTemplateResponse

{

    public Guid Id { get; init; }

}


