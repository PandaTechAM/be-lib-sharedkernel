using MediatR;

namespace SharedKernel.ValidatorAndMediatR;

/// <summary>
///     Marker interface for a CQRS command that returns a response.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>;

/// <summary>
///     Marker interface for a CQRS command that returns no response.
/// </summary>
public interface ICommand : IRequest;

/// <summary>
///     Marker interface for a CQRS query that returns a response.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>;

/// <summary>
///     Marker interface for a CQRS query that returns no response.
/// </summary>
public interface IQuery : IRequest;

/// <summary>
///     Handler contract for a CQRS command that returns a response.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;

/// <summary>
///     Handler contract for a CQRS command that returns no response.
/// </summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand;

/// <summary>
///     Handler contract for a CQRS query that returns a response.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>;

/// <summary>
///     Handler contract for a CQRS query that returns no response.
/// </summary>
public interface IQueryHandler<in TQuery> : IRequestHandler<TQuery>
    where TQuery : IQuery;
