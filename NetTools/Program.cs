using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetTools;

var rootCommand = new ServiceCollection()
    .RegisterServices()
    .BuildServiceProvider()
    .CreateRootCommand();

await rootCommand.Parse(args).InvokeAsync();

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}
