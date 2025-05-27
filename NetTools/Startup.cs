using System.CommandLine;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NetTools.Commands;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console;

namespace NetTools;

/// <summary>
/// Class to register services for dependency injection.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Registers application services and dependencies into the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>This method configures various application services, including command handlers, utilities,
    /// and external service integrations. It also registers implementations for interfaces such as <see
    /// cref="IFileSystem"/>, <see cref="IXmlService"/>, and <see cref="IProcessRunner"/>. Additionally, an HTTP client
    /// for <see cref="INugetService"/> is configured.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to which the services will be added.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, allowing for method chaining.</returns>
    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        services.AddSingleton<StandardizeCommand>();
        services.AddSingleton<RemoveCommand>();
        services.AddSingleton<UpdateCommand>();
        services.AddSingleton<ICsprojHelpers, CsprojHelpers>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<NugetVersionStandardizer>();
        services.AddSingleton<IXmlService, XmlService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IDotnetCommandRunner, DotnetCommandRunner>();
        services.AddSingleton<ISolutionExplorer, SolutionExplorer>();
        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddHttpClient<INugetService, NugetService>();
        services.AddSingleton(AnsiConsole.Console);

        return services;
    }

    /// <summary>
    /// Creates a <see cref="RootCommand"/> instance that serves as the entry point for the command-line interface
    /// Also registers the main commands for the application.
    /// </summary>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/> that provides the required services for command instantiation.
    /// </param>
    /// <returns>
    /// The <see cref="RootCommand"/> instance that contains the application's main commands.
    /// </returns>
    public static RootCommand CreateRootCommand(this IServiceProvider serviceProvider)
        => new("NetTools - A tool to manage and standardize NuGet packages across multiple projects.")
        {
            serviceProvider.GetRequiredService<StandardizeCommand>(),
            serviceProvider.GetRequiredService<RemoveCommand>(),
            serviceProvider.GetRequiredService<UpdateCommand>()
        };
}
