using Fluxor.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Fluxor.Behaviors;

public sealed class LoggingBehavior : IPipelineBehavior
{
    private readonly ILogger<LoggingBehavior> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync<TResponse>(
        ICommand<TResponse> command,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CommandName"] = command.GetType().Name,
            ["CommandType"] = command.GetType().FullName!
        });

        _logger.LogInformation("Handling command");

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogDebug("Handled command in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            if (stopwatch.ElapsedMilliseconds > 500)
            {
                _logger.LogWarning("Slow command detected: {CommandType} took {ElapsedMs}ms", command.GetType().FullName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Command cancelled after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error handling command after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
