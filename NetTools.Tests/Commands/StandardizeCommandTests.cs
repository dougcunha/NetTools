using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using NetTools.Commands;
using NetTools.Services;
using Spectre.Console.Testing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NetTools.Tests.Commands;

[ExcludeFromCodeCoverage]
public sealed class StandardizeCommandTests
{
    private const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
    private const string SOLUTION_DIR = @"C:\TestSolution";
    private const string PROJECT_PATH = @"Project1\Project1.csproj";
    private readonly StandardizeCommand _command;    private readonly TestConsole _console = new();
    private readonly IEnvironmentService _environment = Substitute.For<IEnvironmentService>();
    private readonly INugetVersionStandardizer _standardizer = Substitute.For<INugetVersionStandardizer>();
    private readonly RootCommand _rootCommand = new("nettools");
    private readonly ISolutionExplorer _solutionExplorer = Substitute.For<ISolutionExplorer>();

    public StandardizeCommandTests()
    {
        _console.Interactive();

        _command = new StandardizeCommand
        (
            _standardizer,
            _solutionExplorer,
            _environment
        );

        _rootCommand.AddCommand(_command);
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectNameAndDescription()
    {
        // Assert
        _command.Name.ShouldBe("st");
        _command.Description.ShouldBe("Standardize NuGet package versions in a solution.");
    }

    [Fact]
    public void Constructor_AddsRequiredArguments()
    {
        // Assert
        _command.Arguments.Count.ShouldBe(1);
        _command.Arguments[0].Name.ShouldBe("solutionFile");
    }

    [Fact]
    public void Constructor_AddsRequiredOptions()
    {
        // Assert
        _command.Options.Count.ShouldBe(4);

        var optionNames = _command.Options.Select(static o => o.Name).ToList();
        optionNames.ShouldContain("clean");
        optionNames.ShouldContain("restore");
        optionNames.ShouldContain("build");
        optionNames.ShouldContain("verbose");
    }

    [Fact]
    public async Task HandleAsync_NoProjectsSelected_DoNothing()
    {
        // Arrange
        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns([]);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.DidNotReceive().StandardizeVersions(Arg.Any<StandardizeCommandOptions>(), Arg.Any<string[]>());
    }

    [Fact]
    public async Task HandleAsync_WithProjectsSelected_CallsStandardizer()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                !options.Verbose &&
                !options.Clean &&
                !options.Restore &&
                !options.Build
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithCleanOption_PassesCleanToStandardizer()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE, "--clean"]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                options.Clean &&
                !options.Verbose &&
                !options.Restore &&
                !options.Build
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithRestoreOption_PassesRestoreToStandardizer()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE, "--restore"]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                options.Restore &&
                !options.Verbose &&
                !options.Clean &&
                !options.Build
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithBuildOption_PassesBuildToStandardizer()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE, "--build"]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                options.Build &&
                !options.Verbose &&
                !options.Clean &&
                !options.Restore
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithVerboseOption_PassesVerboseToStandardizer()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE, "--verbose"]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                options.Verbose &&
                !options.Clean &&
                !options.Restore &&
                !options.Build
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithAllOptions_PassesAllParametersCorrectly()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE, "--clean", "--restore", "--build", "--verbose"]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                options.Verbose &&
                options.Clean &&
                options.Restore &&
                options.Build
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithShortOptions_PassesAllParametersCorrectly()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE, "-c", "-r", "-b", "-v"]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                options.Verbose &&
                options.Clean &&
                options.Restore &&
                options.Build
            ),
            PROJECT_PATH
        );
    }

    [Fact]
    public async Task HandleAsync_WithNullSolutionFile_CallsGetOrPromptSolutionFile()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        _solutionExplorer
            .GetOrPromptSolutionFile(null)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st"]);

        // Assert
        result.ShouldBe(0);
        _solutionExplorer.Received(1).GetOrPromptSolutionFile(null);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_MultipleProjects_PassesAllProjectsToStandardizer()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH, "Project2\\Project2.csproj", "Project3\\Project3.csproj" };

        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.InvokeAsync(["st", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(options => 
                options.SolutionFile == SOLUTION_FILE &&
                !options.Verbose &&
                !options.Clean &&
                !options.Restore &&
                !options.Build
            ),
            PROJECT_PATH,
            "Project2\\Project2.csproj",
            "Project3\\Project3.csproj"
        );
    }
}
