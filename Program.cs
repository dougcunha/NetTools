using System.CommandLine;
using System.Diagnostics;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NetTools.Commands;
using NetTools.Services;
using Spectre.Console;

var services = new ServiceCollection();

services.AddSingleton<StandardizeCommand>();
services.AddSingleton<RemoveCommand>();
services.AddSingleton<IFileSystem, FileSystem>();
services.AddSingleton<NugetVersionStandardizer>();
services.AddSingleton<IXDocumentLoader, XDocumentLoader>();
services.AddSingleton<IXmlWriterWrapper, XmlWriterWrapper>();
services.AddSingleton<IProcessRunner, ProcessRunner>();
services.AddSingleton<DotnetCommandRunner>();
services.AddSingleton<SolutionExplorer>();
services.AddSingleton(AnsiConsole.Console);

var rootCommand = new RootCommand("NetTools - A tool to manage and standardize NuGet packages across multiple projects.");

var servicesProvider = services.BuildServiceProvider();

rootCommand.AddCommand(servicesProvider.GetRequiredService<StandardizeCommand>());
rootCommand.AddCommand(servicesProvider.GetRequiredService<RemoveCommand>());

await rootCommand.InvokeAsync(args);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}
