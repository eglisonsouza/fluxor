using Fluxor.Abstractions;
using Fluxor.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Fluxor.Extensions;

public static class CommandPipelineExtensions
{
    public static IServiceCollection AddCommandPipelines(this IServiceCollection services)
    {
        services.AddScoped<ICommandPipeline, Pipeline.CommandPipeline>();

        services.AddScoped<IPipelineBehavior, LoggingBehavior>();
        return services;
    }
}
