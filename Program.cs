using System.CommandLine;
using System.Diagnostics;
using NetTools.Commands;

var rootCommand = new RootCommand("NetTools - A tool to manage and standardize NuGet packages.");
rootCommand.AddCommand(new StandardizeCommand());
rootCommand.AddCommand(new RemoveCommand());

await rootCommand.InvokeAsync(args);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}
