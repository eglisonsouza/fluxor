namespace Fluxor.Abstractions;

public interface ICommandPipeline
{
    Task<TResponse> ExecuteAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
}
