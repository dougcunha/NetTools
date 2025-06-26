using System.IO.Abstractions;
using NetTools.Helpers;
using Spectre.Console;

namespace NetTools.Services;

/// <summary>
/// Service responsible for exploring .sln files and listing associated .csproj files.
/// </summary>
public sealed class SolutionExplorer(IAnsiConsole console, IFileSystem fileSystem) : ISolutionExplorer
{
    /// <summary>
    /// Discovers .csproj files from a given .sln file and allows the user to select projects.
    /// If solutionFile is null or empty, tries to find a .sln in the current directory.
    /// </summary>
    /// <param name="solutionFile">The path to the .sln file (optional).</param>
    /// <param name="markupTitle">Prompt title for project selection.</param>
    /// <param name="notFoundMessage">
    /// A message to display if no .csproj files are found.
    /// </param>
    /// <param name="predicate">Optional filter for csproj files.</param>
    /// <returns>A list of selected .csproj file paths.</returns>
    public List<string> DiscoverAndSelectProjects(string? solutionFile, string markupTitle, string notFoundMessage, Func<string, bool>? predicate = null)
    {
        solutionFile = GetOrPromptSolutionFile(solutionFile);

        if (string.IsNullOrWhiteSpace(solutionFile) || !fileSystem.File.Exists(solutionFile))
        {
            console.Markup("[red]Solution file not found or invalid.[/]\n");

            return [];
        }

        var projectPaths = DiscoverProjectPaths(solutionFile, predicate);

        if (projectPaths.Count == 0)
        {
            console.MarkupLine(notFoundMessage);

            return [];
        }

        return [.. console.Prompt
        (
            new MultiSelectionPrompt<string>()
                .Title(markupTitle)
                .NotRequired()
                .PageSize(20)
                .MoreChoicesText("[grey](Use space to select, enter to confirm)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to select, [green]<enter>[/] to confirm)[/]")
                .AddChoiceGroup("Select all", projectPaths.Order())
        )];
    }

    /// <summary>
    /// Gets the solution file path, prompting the user if not provided.
    /// If solutionFile is null or empty, searches for .sln files in the current directory.
    /// </summary>
    /// <param name="solutionFile">
    /// The path to the .sln file (optional).
    /// </param>
    /// <returns>
    /// The path to the .sln file, or null if not found.
    /// </returns>
    public string? GetOrPromptSolutionFile(string? solutionFile)
    {
        if (!string.IsNullOrWhiteSpace(solutionFile))
            return solutionFile;

        var currentDir = fileSystem.Directory.GetCurrentDirectory();
        var slnFiles = fileSystem.Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly);

        if (slnFiles.Length == 0)
            return null;

        if (slnFiles.Length == 1)
        {
            var found = slnFiles[0];
            console.MarkupLine($"[green]Found solution:[/] [bold]{Path.GetFileName(found)}[/]");

            return found;
        }

        var slnNames = slnFiles
            .Select(Path.GetFileName)
            .Where(static n => !string.IsNullOrWhiteSpace(n))
            .Select(static n => n!).ToList();

        var chosen = console.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Select the solution file:[/]")
                .PageSize(10)
                .AddChoices(slnNames)
        );

        return string.IsNullOrWhiteSpace(chosen)
            ? null
            : Path.Combine(currentDir, chosen);
    }

    private List<string> DiscoverProjectPaths(string solutionFile, Func<string, bool>? predicate)
    {
        var projectPaths = new List<string>();
        var lines = fileSystem.File.ReadLines(solutionFile).ToList();

        using var spinner = AsyncSpinner.Show(console, "Reading solution file...");

        foreach (var line in lines.Where(IsProjectLine))
        {
            var parts = line.Split('"');

            if (parts.Length <= 5)
                continue;

            var relativePath = parts[5];
            var absolutePath = Path.Combine(Path.GetDirectoryName(solutionFile)!, relativePath);

            if (predicate?.Invoke(absolutePath) is false)
                continue;

            projectPaths.Add(relativePath);
        }

        return projectPaths;
    }

    private static bool IsProjectLine(string line)
        => line.Trim().StartsWith("Project(", StringComparison.OrdinalIgnoreCase) && line.Contains(".csproj");
}
