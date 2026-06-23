namespace BuildingBlocks.Application.Abstractions;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }
}
