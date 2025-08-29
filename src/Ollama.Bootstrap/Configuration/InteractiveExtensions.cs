using Microsoft.Extensions.DependencyInjection;
using Ollama.Domain.Contracts;
using Ollama.Infrastructure.Interceptors;
using Ollama.Infrastructure.Interactive;

namespace Ollama.Bootstrap.Configuration;

/// <summary>
/// Configuration extensions for interactive mode services
/// </summary>
public static class InteractiveExtensions
{
    /// <summary>
    /// Add interactive mode services and interceptors to the container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInteractiveMode(this IServiceCollection services)
    {
        // Register interceptor system
        services.AddSingleton<IInterceptorDispatcher, InterceptorDispatcher>();
        
        // Register interceptors (order matters - lower priority executes first)
        services.AddTransient<IMessageInterceptor, CommandInterceptor>();
        services.AddTransient<IMessageInterceptor, ConsoleInterceptor>();
        
        // Register session handler
        services.AddTransient<InteractiveSessionHandler>();
        
        return services;
    }

    /// <summary>
    /// Configure the interceptor chain with default interceptors
    /// Call this after building the service provider
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <returns>Service provider for chaining</returns>
    public static IServiceProvider ConfigureInterceptorChain(this IServiceProvider serviceProvider)
    {
        var dispatcher = serviceProvider.GetRequiredService<IInterceptorDispatcher>();
        var interceptors = serviceProvider.GetServices<IMessageInterceptor>();
        
        foreach (var interceptor in interceptors)
        {
            dispatcher.RegisterInterceptor(interceptor);
        }
        
        return serviceProvider;
    }

    /// <summary>
    /// Add custom interceptor to the chain
    /// </summary>
    /// <typeparam name="T">Interceptor type</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddInterceptor<T>(this IServiceCollection services)
        where T : class, IMessageInterceptor
    {
        services.AddTransient<IMessageInterceptor, T>();
        return services;
    }
}
