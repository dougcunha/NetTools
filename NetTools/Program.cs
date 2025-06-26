using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace NetTools;

[ExcludeFromCodeCoverage]
file static class Program
{
    private static async Task Main(string[] args)
    {
        var rootCommand = new ServiceCollection()
            .RegisterServices()
            .BuildServiceProvider()
            .CreateRootCommand();

        await rootCommand.Parse(args).InvokeAsync().ConfigureAwait(false);

        if (Debugger.IsAttached)
        {
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }
    }
}