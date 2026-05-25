using Fluxor.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace Fluxor.Pipeline;

public sealed class CommandPipeline : ICommandPipeline
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IPipelineBehavior> _behaviors;
    private static readonly ConcurrentDictionary<Type, MethodInfo> _handleMethods = new();

    public CommandPipeline(IServiceProvider serviceProvider, IEnumerable<IPipelineBehavior> behaviors)
    {
        _serviceProvider = serviceProvider;
        _behaviors = behaviors;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType) ?? throw new InvalidOperationException(
                $"Handler not found for command {command.GetType().Name}");

        var handleMethod = _handleMethods.GetOrAdd(handlerType, type =>
            type.GetMethod("HandleAsync") ?? throw new InvalidOperationException(
                    $"HandleAsync not found on {type.Name}"));

        Func<Task<TResponse>> handlerFunc = () =>
            (Task<TResponse>)handleMethod.Invoke(handler, [command, cancellationToken])!;

        foreach (var behavior in _behaviors.Reverse())
        {
            var next = handlerFunc;
            handlerFunc = () => behavior.HandleAsync(command, next, cancellationToken);
        }

        return await handlerFunc();
    }
}
