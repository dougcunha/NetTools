using Spectre.Console;

namespace NetTools.Services;

/// <summary>
/// Service responsible for exploring .sln files and listing associated .csproj files.
/// </summary>
public static class SolutionExplorer
{
    /// <summary>
    /// Discovers .csproj files from a given .sln file and allows the user to select projects.
    /// </summary>
    /// <param name="solutionFile">The path to the .sln file.</param>
    /// <returns>A list of selected .csproj file paths.</returns>
    public static List<string> DiscoverAndSelectProjects(string solutionFile, string markupTitle, Func<string, bool>? predicate = null)
    {
        if (string.IsNullOrWhiteSpace(solutionFile) || !File.Exists(solutionFile))
        {
            AnsiConsole.Markup("[red]Solution file not found or invalid.[/]\n");

            return [];
        }

        var projectPaths = new List<string>();

        var progress = AnsiConsole.Progress()
            .AutoClear(true)
            .Columns
            (                    
                new ProgressBarColumn
                {
                    CompletedStyle = new Style(foreground: Color.Green1, decoration: Decoration.Conceal | Decoration.Bold | Decoration.Invert),
                    RemainingStyle = new Style(decoration: Decoration.Conceal),
                    FinishedStyle = new Style(foreground: Color.Green1, decoration: Decoration.Conceal | Decoration.Bold | Decoration.Invert)
                },
                new PercentageColumn(),
                new SpinnerColumn(),
                new ElapsedTimeColumn(),
                new TaskDescriptionColumn()
            );

        progress.Start(ctx =>
        {
            var lines = File.ReadLines(solutionFile).ToList();
            var task = ctx.AddTask("Discovering .csproj files", maxValue: lines.Count);                
            task.StartTask();
            
            foreach (var line in lines)
            {
                if (!line.Trim().StartsWith("Project(") || !line.Contains(".csproj"))
                {
                    task.Increment(1);

                    continue;
                }

                var parts = line.Split('"');

                if (parts.Length > 5)
                {
                    var relativePath = parts[5];
                    var absolutePath = Path.Combine(Path.GetDirectoryName(solutionFile)!, relativePath);

                    if (predicate != null && !predicate(absolutePath))
                    {
                        task.Increment(1);

                        continue;
                    }

                    projectPaths.Add(relativePath);
                }

                task.Increment(1);
            }
        });

        if (projectPaths.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No .csproj files found in the solution file.[/]");

            return [];
        }

        var selectedProjects = AnsiConsole.Prompt
        (
            new MultiSelectionPrompt<string>()
                .Title(markupTitle)
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Use space to select, enter to confirm)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                .AddChoiceGroup("Select all", projectPaths.OrderBy(p => p))
        );

        return selectedProjects;
    }
}
