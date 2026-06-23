using BuildingBlocks.Application.CQRS;

namespace ApiHost.Diagnostics;

public sealed record TestPipelineQuery(string Name) : IQuery<string>;
