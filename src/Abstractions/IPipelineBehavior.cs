namespace Fluxor.Abstractions;

public interface IPipelineBehavior
{
    Task<TResponse> HandleAsync<TResponse>(ICommand<TResponse> command, Func<Task<TResponse>> next, CancellationToken cancellationToken);
}
