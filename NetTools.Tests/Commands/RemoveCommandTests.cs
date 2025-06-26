using NetTools.Commands;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console.Testing;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using NetTools.Tests.Helpers;

namespace NetTools.Tests.Commands;

[ExcludeFromCodeCoverage]
public sealed class RemoveCommandTests
{
    private readonly TestConsole _console = new();
    private readonly ISolutionExplorer _solutionExplorer = Substitute.For<ISolutionExplorer>();
    private readonly ICsprojHelpers _csprojHelpers = Substitute.For<ICsprojHelpers>();
    private readonly IDotnetCommandRunner _dotnetRunner = Substitute.For<IDotnetCommandRunner>();
    private readonly IEnvironmentService _environment = Substitute.For<IEnvironmentService>();
    private readonly RemoveCommand _command;
    private static readonly string _packageID = "TestPackage".NormalizePath();
    private static readonly string _solutionFile = "TestSolution/Solution.sln".NormalizePath();
    private static readonly string _projectPath = "Project1/Project1.csproj".NormalizePath();
    private static readonly string _fullProjectPath = "TestSolution/Project1/Project1.csproj".NormalizePath();

    public RemoveCommandTests()
    {
        _console.Interactive();
        _command = new RemoveCommand(_console, _solutionExplorer, _csprojHelpers, _dotnetRunner, _environment);
    }

    [Fact]
    public void Constructor_CreatesCommandWithCorrectNameAndDescription()
    {
        // Assert
        _command.Name.ShouldBe("rm");
        _command.Description.ShouldBe("Remove a NuGet package from selected projects in a solution.");
    }

    [Fact]
    public void Constructor_AddsRequiredArguments()
    {
        // Assert
        _command.Arguments.Count.ShouldBe(2);
        _command.Arguments[0].Name.ShouldBe("packageId");
        _command.Arguments[^1].Name.ShouldBe("solutionFile");
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
    public async Task Invoke_ValidPackageAndSolution_RemovesPackageFromSelectedProjects()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        _csprojHelpers.HasPackage(_fullProjectPath, _packageID).Returns(true);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse
        (
            [
                "rm",
                _packageID,
                _solutionFile
            ]
        ).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.Received(1).RemovePackageFromCsproj(_fullProjectPath, _packageID);
        _console.Output.ShouldContain($"Removed '{_packageID}' from {_projectPath}.");
    }

    [Fact]
    public async Task Invoke_NoProjectsSelected_DoesNotRemovePackages()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse([
            "rm",
            _packageID,
            _solutionFile
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.DidNotReceive().RemovePackageFromCsproj(Arg.Any<string>(), Arg.Any<string>());
        _dotnetRunner.DidNotReceive().RunSequentialCommands(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task Invoke_MultipleProjects_RemovesPackageFromAllSelected()
    {
        // Arrange
        string[] projectPaths =
        [
            "Project1/Project1.csproj".NormalizePath(),
            "Project2/Project2.csproj".NormalizePath()
        ];

        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([.. projectPaths]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.Received(1).RemovePackageFromCsproj("TestSolution/Project1/Project1.csproj".NormalizePath(), _packageID);
        _csprojHelpers.Received(1).RemovePackageFromCsproj("TestSolution/Project2/Project2.csproj".NormalizePath(), _packageID);
        _console.Output.ShouldContain($"Removed '{_packageID}' from Project1/Project1.csproj.".NormalizePath());
        _console.Output.ShouldContain($"Removed '{_packageID}' from Project2/Project2.csproj.".NormalizePath());
    }

    [Fact]
    public async Task Invoke_WithCleanOption_RunsDotnetCommandsWithClean()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile,
            "--clean"
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "Solution.sln",
            verbose: false,
            clean: true,
            restore: false,
            build: false
        );
    }

    [Fact]
    public async Task Invoke_WithRestoreOption_RunsDotnetCommandsWithRestore()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile,
            "--restore"
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "Solution.sln",
            verbose: false,
            clean: false,
            restore: true,
            build: false
        );
    }

    [Fact]
    public async Task Invoke_WithBuildOption_RunsDotnetCommandsWithBuild()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile,
            "--build"
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "Solution.sln",
            false, // verbose
            false, // clean
            false, // restore
            true   // build
        );
    }

    [Fact]
    public async Task Invoke_WithVerboseOption_RunsDotnetCommandsWithVerbose()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile,
            "--verbose"
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "Solution.sln",
            verbose: true,
            clean: false,
            restore: false,
            build: false
        );
    }

    [Fact]
    public async Task Invoke_WithAllOptions_RunsDotnetCommandsWithAllFlags()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile,
            "--clean",
            "--restore",
            "--build",
            "--verbose"
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "Solution.sln",
            true, // verbose
            true, // clean
            true, // restore
            true  // build
        );
    }

    [Fact]
    public async Task Invoke_WithShortOptions_RunsDotnetCommandsCorrectly()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile,
            "-c", "-r", "-b", "-v"
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "Solution.sln",
            true, // verbose
            true, // clean
            true, // restore
            true  // build
        );
    }

    [Fact]
    public async Task Invoke_WithNullSolutionFile_UsesPromptedSolution()
    {
        // Arrange
        var promptedSolution = "TestSolution/FoundSolution.sln".NormalizePath();
        _solutionExplorer.GetOrPromptSolutionFile(null).Returns(promptedSolution);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            promptedSolution,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse([
            "rm",
            _packageID
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _solutionExplorer.Received(1).GetOrPromptSolutionFile(null);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "TestSolution",
            "FoundSolution.sln",
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<bool>()
        );
    }

    [Fact]
    public async Task Invoke_ChangesCurrentDirectoryToSolutionDirectory()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse(
        [
            "rm",
            _packageID,
            _solutionFile
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _environment.CurrentDirectory.ShouldBe("TestSolution");
    }

    [Fact]
    public async Task Invoke_UsesPredicateToFilterProjectsWithPackage()
    {
        // Arrange
        _solutionExplorer.GetOrPromptSolutionFile(_solutionFile).Returns(_solutionFile);

        // Configure the predicate to be called with HasPackage check
        _solutionExplorer.DiscoverAndSelectProjects
        (
            _solutionFile,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<string, bool>>(static predicate =>
            {
                // Verify the predicate calls HasPackage
                predicate(_fullProjectPath);
            })
        ).Returns([_projectPath]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.Parse([
            "rm",
            _packageID,
            _solutionFile
        ]).InvokeAsync();

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.Received().HasPackage(_fullProjectPath, _packageID);
    }
}
