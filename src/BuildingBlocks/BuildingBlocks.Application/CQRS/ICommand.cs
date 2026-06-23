using BuildingBlocks.Application.Results;
using MediatR;

namespace BuildingBlocks.Application.CQRS;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
