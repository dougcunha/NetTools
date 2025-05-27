﻿using System.CommandLine;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetTools;

var rootCommand = new ServiceCollection()
    .RegisterServices()
    .BuildServiceProvider()
    .CreateRootCommand();

await rootCommand.InvokeAsync(args).ConfigureAwait(false);

if (Debugger.IsAttached)
{
    Console.WriteLine("Press any key to exit...");
    Console.Read();
}
