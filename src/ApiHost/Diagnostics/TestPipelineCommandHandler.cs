using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;

namespace ApiHost.Diagnostics;

public sealed class TestPipelineCommandHandler : IRequestHandler<TestPipelineCommand, Result<string>>
{
    public Task<Result<string>> Handle(TestPipelineCommand request, CancellationToken cancellationToken) =>
        Task.FromResult(Result<string>.Success("OK"));
}
