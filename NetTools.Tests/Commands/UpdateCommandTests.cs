using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using NetTools.Commands;
using NetTools.Helpers;
using NetTools.Models;
using NetTools.Services;
using Spectre.Console.Testing;

namespace NetTools.Tests.Commands;

[ExcludeFromCodeCoverage]
public sealed class UpdateCommandTests
{
    private const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
    private const string SOLUTION_DIR = @"C:\TestSolution";
    private const string PROJECT_PATH = @"Project1\Project1.csproj";
    private readonly UpdateCommand _command;

    private readonly TestConsole _console = new();
    private readonly ICsprojHelpers _csprojHelpers = Substitute.For<ICsprojHelpers>();
    private readonly IDotnetCommandRunner _dotnetRunner = Substitute.For<IDotnetCommandRunner>();
    private readonly IEnvironmentService _environment = Substitute.For<IEnvironmentService>();
    private readonly INugetService _nugetService = Substitute.For<INugetService>();
    private readonly RootCommand _rootCommand = new("nettools");
    private readonly ISolutionExplorer _solutionExplorer = Substitute.For<ISolutionExplorer>();

    public UpdateCommandTests()
    {
        _console.Interactive();

        _command = new UpdateCommand
        (
            _solutionExplorer,
            _nugetService,
            _console,
            _csprojHelpers,
            _dotnetRunner,
            _environment
        );

        _rootCommand.AddCommand(_command);
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectNameAndDescription()
    {
        // Assert
        _command.Name.ShouldBe("upd");
        _command.Description.ShouldBe("Check for NuGet package updates in selected projects.");
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
        _command.Options.Count.ShouldBe(5);

        var optionNames = _command.Options.Select(static o => o.Name).ToList();
        optionNames.ShouldContain("include-prerelease");
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
                "[green]Select the projects to check for updates:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns([]);

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_AllPackagesUpToDate_ReturnsOneAndDisplaysMessage()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "1.0.0"
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("All packages are up to date.");
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_OutdatedPackagesButNoneSelected_ReturnsOneAndDisplaysMessage()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);
        _console.Output.ShouldContain("No packages selected for update.");
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_OutdatedPackagesSelected_UpdatesAndReturnsZero()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _dotnetRunner
            .RunSequentialCommands(SOLUTION_DIR, "Solution.sln")
            .Returns(true);

        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE]);

        // Assert
        result.ShouldBe(0);

        _csprojHelpers
            .ReceivedWithAnyArgs(1)
            .UpdatePackagesInProjects(projectPackages, latestVersions, Arg.Any<List<(string Name, string Id)>>());

        _console.Output.ShouldContain("Selected packages updated successfully.");
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_WithIncludePrereleaseOption_PassesToNugetService()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0-beta"
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs([]);

        _nugetService
            .GetLatestVersionAsync("TestPackage", true)
            .Returns("2.0.0-beta");

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE, "--include-prerelease"]);

        // Assert
        result.ShouldBe(0);
        await _nugetService.Received(1).GetLatestVersionAsync("TestPackage", true);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_WithCleanOption_PassesToDotnetRunner()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _dotnetRunner
            .RunSequentialCommands(SOLUTION_DIR, "Solution.sln", false, true)
            .Returns(true);

        _console.Input.PushTextWithEnter(" ");

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE, "--clean"]);

        // Assert
        result.ShouldBe(0);
        _dotnetRunner.Received(1).RunSequentialCommands(SOLUTION_DIR, "Solution.sln", false, true);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_WithRestoreOption_PassesToDotnetRunner()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _dotnetRunner
            .RunSequentialCommands(SOLUTION_DIR, "Solution.sln", false, false, true)
            .Returns(true);

        _console.Input.PushTextWithEnter(" ");

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE, "--restore"]);

        // Assert
        result.ShouldBe(0);
        _dotnetRunner.Received(1).RunSequentialCommands(SOLUTION_DIR, "Solution.sln", false, false, true);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_WithBuildOption_PassesToDotnetRunner()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _dotnetRunner
            .RunSequentialCommands(SOLUTION_DIR, "Solution.sln", false, false, false, true)
            .Returns(true);

        _console.Input.PushTextWithEnter(" ");

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE, "--build"]);

        // Assert
        result.ShouldBe(0);
        _dotnetRunner.Received(1).RunSequentialCommands(SOLUTION_DIR, "Solution.sln", false, false, false, true);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_WithVerboseOption_PassesToDotnetRunner()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _dotnetRunner
            .RunSequentialCommands(SOLUTION_DIR, "Solution.sln", true)
            .Returns(true);

        _console.Input.PushTextWithEnter(" ");

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE, "--verbose"]);

        // Assert
        result.ShouldBe(0);
        _dotnetRunner.Received(1).RunSequentialCommands(SOLUTION_DIR, "Solution.sln", true);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    [Fact]
    public async Task HandleAsync_WithAllOptions_PassesAllParametersCorrectly()
    {
        // Arrange
        var projects = new List<string> { PROJECT_PATH };

        var projectPackages = new Dictionary<string, List<Package>>
        {
            [PROJECT_PATH] = [new Package("TestPackage", "1.0.0")]
        };

        var consolidatedPackages = new Dictionary<string, string>
        {
            ["TestPackage"] = "1.0.0"
        };

        var latestVersions = new Dictionary<string, string?>
        {
            ["TestPackage"] = "2.0.0-beta"
        };

        var outdatedPackages = new List<(string PackageId, string Installed, string? Latest)>
        {
            ("TestPackage", "1.0.0", "2.0.0-beta")
        };

        SetupBasicMocks(projects, projectPackages, consolidatedPackages, latestVersions);

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs(outdatedPackages);

        _dotnetRunner
            .RunSequentialCommands(SOLUTION_DIR, "Solution.sln", true, true, true, true)
            .Returns(true);

        _nugetService
            .GetLatestVersionAsync("TestPackage", true)
            .Returns("2.0.0-beta");

        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Enter);

        // Act
        int result = await _rootCommand.InvokeAsync(["upd", SOLUTION_FILE, "--include-prerelease", "--clean", "--restore", "--build", "--verbose"]);

        // Assert
        result.ShouldBe(0);
        await _nugetService.Received(1).GetLatestVersionAsync("TestPackage", true);
        _dotnetRunner.Received(1).RunSequentialCommands(SOLUTION_DIR, "Solution.sln", true, true, true, true);
        _environment.Received(1).CurrentDirectory = SOLUTION_DIR;
    }

    private void SetupBasicMocks
    (
        List<string> projects,
        Dictionary<string, List<Package>> projectPackages,
        Dictionary<string, string> consolidatedPackages,
        Dictionary<string, string?> latestVersions
    )
    {
        _solutionExplorer
            .GetOrPromptSolutionFile(SOLUTION_FILE)
            .Returns(SOLUTION_FILE);

        _solutionExplorer
            .DiscoverAndSelectProjects
            (
                SOLUTION_FILE,
                "[green]Select the projects to check for updates:[/]",
                "[yellow]No .csproj files found in the solution file.[/]"
            )
            .Returns(projects);

        _csprojHelpers
            .GetPackagesFromProjects(projects)
            .Returns(projectPackages);

        _csprojHelpers
            .RetrieveUniquePackageVersions(projectPackages)
            .Returns(consolidatedPackages);

        foreach (var package in consolidatedPackages)
        {
            _nugetService
                .GetLatestVersionAsync(package.Key)
                .Returns(latestVersions[package.Key]);
        }

        _csprojHelpers
            .GetOutdatedPackages(consolidatedPackages, latestVersions)
            .ReturnsForAnyArgs([]);
    }
}