using BuildingBlocks.Application.CQRS;

namespace ApiHost.Diagnostics;

public sealed record TestPipelineCommand(string Name) : ICommand<string>;
