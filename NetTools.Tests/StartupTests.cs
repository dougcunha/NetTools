using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NetTools.Commands;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console;

namespace NetTools.Tests;

[ExcludeFromCodeCoverage]
public sealed class StartupTests
{
    [Fact]
    public void RegisterServices_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify all singleton services are registered
        serviceProvider.GetService<StandardizeCommand>().ShouldNotBeNull();
        serviceProvider.GetService<RemoveCommand>().ShouldNotBeNull();
        serviceProvider.GetService<UpdateCommand>().ShouldNotBeNull();
        serviceProvider.GetService<ICsprojHelpers>().ShouldNotBeNull();
        serviceProvider.GetService<IFileSystem>().ShouldNotBeNull();
        serviceProvider.GetService<INugetVersionStandardizer>().ShouldNotBeNull();
        serviceProvider.GetService<IXmlService>().ShouldNotBeNull();
        serviceProvider.GetService<IProcessRunner>().ShouldNotBeNull();
        serviceProvider.GetService<IDotnetCommandRunner>().ShouldNotBeNull();
        serviceProvider.GetService<ISolutionExplorer>().ShouldNotBeNull();
        serviceProvider.GetService<IEnvironmentService>().ShouldNotBeNull();
        serviceProvider.GetService<INugetService>().ShouldNotBeNull();
        serviceProvider.GetService<IAnsiConsole>().ShouldNotBeNull();
    }

    [Fact]
    public void RegisterServices_ShouldRegisterCorrectImplementations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify correct implementations are registered
        serviceProvider.GetService<ICsprojHelpers>().ShouldBeOfType<CsprojHelpers>();
        serviceProvider.GetService<IFileSystem>().ShouldBeOfType<FileSystem>();
        serviceProvider.GetService<INugetVersionStandardizer>().ShouldBeOfType<NugetVersionStandardizer>();
        serviceProvider.GetService<IXmlService>().ShouldBeOfType<XmlService>();
        serviceProvider.GetService<IProcessRunner>().ShouldBeOfType<ProcessRunner>();
        serviceProvider.GetService<IDotnetCommandRunner>().ShouldBeOfType<DotnetCommandRunner>();
        serviceProvider.GetService<ISolutionExplorer>().ShouldBeOfType<SolutionExplorer>();
        serviceProvider.GetService<IEnvironmentService>().ShouldBeOfType<EnvironmentService>();
        serviceProvider.GetService<INugetService>().ShouldBeOfType<NugetService>();
        serviceProvider.GetService<IAnsiConsole>().ShouldBe(AnsiConsole.Console);
    }

    [Fact]
    public void RegisterServices_ShouldRegisterServicesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify singleton behavior - same instance should be returned
        var csprojHelpers1 = serviceProvider.GetService<ICsprojHelpers>();
        var csprojHelpers2 = serviceProvider.GetService<ICsprojHelpers>();
        csprojHelpers1.ShouldBeSameAs(csprojHelpers2);

        var xmlService1 = serviceProvider.GetService<IXmlService>();
        var xmlService2 = serviceProvider.GetService<IXmlService>();
        xmlService1.ShouldBeSameAs(xmlService2);

        var standardizer1 = serviceProvider.GetService<INugetVersionStandardizer>();
        var standardizer2 = serviceProvider.GetService<INugetVersionStandardizer>();
        standardizer1.ShouldBeSameAs(standardizer2);
    }

    [Fact]
    public void RegisterServices_ShouldRegisterHttpClientForNugetService()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        httpClientFactory.ShouldNotBeNull();

        // Verify NugetService can be created (which uses HttpClient)
        var nugetService = serviceProvider.GetService<INugetService>();
        nugetService.ShouldNotBeNull();
        nugetService.ShouldBeOfType<NugetService>();
    }

    [Fact]
    public void RegisterServices_ShouldReturnSameServiceCollectionInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.RegisterServices();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void RegisterServices_ShouldRegisterAllRequiredInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that all required interfaces are registered
        var registeredInterfaces = new[]
        {
            typeof(ICsprojHelpers),
            typeof(IFileSystem),
            typeof(INugetVersionStandardizer),
            typeof(IXmlService),
            typeof(IProcessRunner),
            typeof(IDotnetCommandRunner),
            typeof(ISolutionExplorer),
            typeof(IEnvironmentService),
            typeof(INugetService),
            typeof(IAnsiConsole)
        };

        foreach (var interfaceType in registeredInterfaces)
        {
            var service = serviceProvider.GetService(interfaceType);
            service.ShouldNotBeNull($"{interfaceType.Name} should be registered");
        }
    }

    [Fact]
    public void CreateRootCommand_ShouldCreateRootCommandWithCorrectDescription()
    {
        // Arrange
        var services = new ServiceCollection();
        services.RegisterServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var rootCommand = serviceProvider.CreateRootCommand();

        // Assert
        rootCommand.ShouldNotBeNull();
        rootCommand.Description.ShouldBe("NetTools - A tool to manage and standardize NuGet packages across multiple projects.");
    }

    [Fact]
    public void CreateRootCommand_ShouldContainAllRequiredCommands()
    {
        // Arrange
        var services = new ServiceCollection();
        services.RegisterServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var rootCommand = serviceProvider.CreateRootCommand();

        // Assert
        rootCommand.Subcommands.Count.ShouldBe(3);

        var commandNames = rootCommand.Subcommands.Select(c => c.Name).ToList();
        commandNames.ShouldContain("st"); // StandardizeCommand
        commandNames.ShouldContain("rm"); // RemoveCommand
        commandNames.ShouldContain("upd"); // UpdateCommand
    }

    [Fact]
    public void CreateRootCommand_ShouldReturnSameInstanceOfCommands()
    {
        // Arrange
        var services = new ServiceCollection();
        services.RegisterServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var rootCommand1 = serviceProvider.CreateRootCommand();
        var rootCommand2 = serviceProvider.CreateRootCommand();

        // Assert
        // Commands should be the same instances (singletons)
        var standardizeCommand1 = rootCommand1.Subcommands.First(c => c.Name == "st");
        var standardizeCommand2 = rootCommand2.Subcommands.First(c => c.Name == "st");
        standardizeCommand1.ShouldBeSameAs(standardizeCommand2);

        var removeCommand1 = rootCommand1.Subcommands.First(c => c.Name == "rm");
        var removeCommand2 = rootCommand2.Subcommands.First(c => c.Name == "rm");
        removeCommand1.ShouldBeSameAs(removeCommand2);

        var updateCommand1 = rootCommand1.Subcommands.First(c => c.Name == "upd");
        var updateCommand2 = rootCommand2.Subcommands.First(c => c.Name == "upd");
        updateCommand1.ShouldBeSameAs(updateCommand2);
    }

    [Fact]
    public void CreateRootCommand_ShouldBeAbleToResolveAllCommandDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.RegisterServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        // This should not throw any exceptions if all dependencies are properly registered
        var standardizeCommand = serviceProvider.GetRequiredService<StandardizeCommand>();
        standardizeCommand.ShouldNotBeNull();

        var removeCommand = serviceProvider.GetRequiredService<RemoveCommand>();
        removeCommand.ShouldNotBeNull();

        var updateCommand = serviceProvider.GetRequiredService<UpdateCommand>();
        updateCommand.ShouldNotBeNull();
    }

    [Fact]
    public void RegisterServices_ShouldAllowCircularDependencyResolution()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.RegisterServices();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Verify that complex dependency graphs can be resolved
        // (some services might depend on each other)
        Should.NotThrow(() =>
        {
            var standardizeCommand = serviceProvider.GetRequiredService<StandardizeCommand>();
            var removeCommand = serviceProvider.GetRequiredService<RemoveCommand>();
            var updateCommand = serviceProvider.GetRequiredService<UpdateCommand>();

            // All commands should be successfully created
            standardizeCommand.ShouldNotBeNull();
            removeCommand.ShouldNotBeNull();
            updateCommand.ShouldNotBeNull();
        });
    }
}
