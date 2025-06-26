using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using NetTools.Commands;
using NetTools.Services;
using NetTools.Tests.Helpers;
using Spectre.Console.Testing;

namespace NetTools.Tests.Commands;

[ExcludeFromCodeCoverage]
public sealed class StandardizeCommandTests
{
    private static readonly string _solutionFile = "/TestSolution/Solution.sln".NormalizePath();
    private static readonly string _solutionDir = "/TestSolution".NormalizePath();
    private static readonly string _projectPath = "/Project1/Project1.csproj".NormalizePath();
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

        _rootCommand.Subcommands.Add(_command);
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
        optionNames.ShouldContain("--clean");
        optionNames.ShouldContain("--restore");
        optionNames.ShouldContain("--build");
        optionNames.ShouldContain("--verbose");
    }

    [Fact]
    public async Task HandleAsync_NoProjectsSelected_DoNothing()
    {
        // Arrange
        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns([]);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;
        _standardizer.DidNotReceive().StandardizeVersions(Arg.Any<StandardizeCommandOptions>(), Arg.Any<string[]>());
    }

    [Fact]
    public async Task HandleAsync_WithProjectsSelected_CallsStandardizer()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>
            (
                static options =>
                options.SolutionFile == _solutionFile &&
                !options.Verbose &&
                !options.Clean &&
                !options.Restore &&
                !options.Build
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithCleanOption_PassesCleanToStandardizer()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile, "--clean"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                options.Clean &&
                !options.Verbose &&
                !options.Restore &&
                !options.Build
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithRestoreOption_PassesRestoreToStandardizer()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile, "--restore"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                options.Restore &&
                !options.Verbose &&
                !options.Clean &&
                !options.Build
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithBuildOption_PassesBuildToStandardizer()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile, "--build"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                options.Build &&
                !options.Verbose &&
                !options.Clean &&
                !options.Restore
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithVerboseOption_PassesVerboseToStandardizer()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile, "--verbose"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                options.Verbose &&
                !options.Clean &&
                !options.Restore &&
                !options.Build
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithAllOptions_PassesAllParametersCorrectly()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile, "--clean", "--restore", "--build", "--verbose"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                options.Verbose &&
                options.Clean &&
                options.Restore &&
                options.Build
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithShortOptions_PassesAllParametersCorrectly()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile, "-c", "-r", "-b", "-v"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions
        (
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                options.Verbose &&
                options.Clean &&
                options.Restore &&
                options.Build
            ),
            _projectPath
        );
    }

    [Fact]
    public async Task HandleAsync_WithNullSolutionFile_CallsGetOrPromptSolutionFile()
    {
        // Arrange
        var projects = new List<string> { _projectPath };

        _solutionExplorer
            .GetOrPromptSolutionFile(null)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st"]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _solutionExplorer.Received(1).GetOrPromptSolutionFile(null);
        _environment.Received(1).CurrentDirectory = _solutionDir;
    }

    [Fact]
    public async Task HandleAsync_MultipleProjects_PassesAllProjectsToStandardizer()
    {
        // Arrange
        var projects = new List<string> { _projectPath, "Project2/Project2.csproj".NormalizePath(), "Project3/Project3.csproj".NormalizePath() };

        _solutionExplorer
            .GetOrPromptSolutionFile(_solutionFile)
            .Returns(_solutionFile);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                _solutionFile,
                "[green]Select the projects to standardize:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        // Act
        int result = await _rootCommand.Parse(["st", _solutionFile]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = _solutionDir;

        _standardizer.Received(1).StandardizeVersions(
            Arg.Is<StandardizeCommandOptions>(static options =>
                options.SolutionFile == _solutionFile &&
                !options.Verbose &&
                !options.Clean &&
                !options.Restore &&
                !options.Build
            ),
            _projectPath,
            "Project2/Project2.csproj".NormalizePath(),
            "Project3/Project3.csproj".NormalizePath()
        );
    }
}
