using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;

namespace ApiHost.Diagnostics;

public sealed class TestPipelineQueryHandler : IRequestHandler<TestPipelineQuery, Result<string>>
{
    public Task<Result<string>> Handle(TestPipelineQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<string>.Success($"Query:{request.Name}"));
}
