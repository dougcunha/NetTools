using NetTools.Commands;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console.Testing;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

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
        optionNames.ShouldContain("clean");
        optionNames.ShouldContain("restore");
        optionNames.ShouldContain("build");
        optionNames.ShouldContain("verbose");
    }

    [Fact]
    public async Task Invoke_ValidPackageAndSolution_RemovesPackageFromSelectedProjects()
    {        // Arrange
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";
        const string FULL_PROJECT_PATH = @"C:\TestSolution\Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        _csprojHelpers.HasPackage(FULL_PROJECT_PATH, PACKAGE_ID).Returns(true);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE
        ]);

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.Received(1).RemovePackageFromCsproj(FULL_PROJECT_PATH, PACKAGE_ID);
        _console.Output.ShouldContain($"Removed '{PACKAGE_ID}' from {PROJECT_PATH}.");
    }

    [Fact]
    public async Task Invoke_NoProjectsSelected_DoesNotRemovePackages()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE
        ]);

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.DidNotReceive().RemovePackageFromCsproj(Arg.Any<string>(), Arg.Any<string>());
        _dotnetRunner.DidNotReceive().RunSequentialCommands(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task Invoke_MultipleProjects_RemovesPackageFromAllSelected()
    {        // Arrange
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";

        string[] projectPaths =
        [
            @"Project1\Project1.csproj",
            @"Project2\Project2.csproj"
        ];

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns(projectPaths.ToList());

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE
        ]);

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.Received(1).RemovePackageFromCsproj(@"C:\TestSolution\Project1\Project1.csproj", PACKAGE_ID);
        _csprojHelpers.Received(1).RemovePackageFromCsproj(@"C:\TestSolution\Project2\Project2.csproj", PACKAGE_ID);
        _console.Output.ShouldContain($"Removed '{PACKAGE_ID}' from Project1\\Project1.csproj.");
        _console.Output.ShouldContain($"Removed '{PACKAGE_ID}' from Project2\\Project2.csproj.");
    }

    [Fact]
    public async Task Invoke_WithCleanOption_RunsDotnetCommandsWithClean()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE,
            "--clean"
        ]);

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE,
            "--restore"
        ]);

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE,
            "--build"
        ]);

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE,
            "--verbose"
        ]);

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE,
            "--clean",
            "--restore",
            "--build",
            "--verbose"
        ]);

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE,
            "-c", "-r", "-b", "-v"
        ]);

        // Assert
        result.ShouldBe(0);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string PROMPTED_SOLUTION = @"C:\TestSolution\FoundSolution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(null).Returns(PROMPTED_SOLUTION);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            PROMPTED_SOLUTION,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync([
            "rm",
            PACKAGE_ID
        ]);

        // Assert
        result.ShouldBe(0);
        _solutionExplorer.Received(1).GetOrPromptSolutionFile(null);

        _dotnetRunner.Received(1).RunSequentialCommands
        (
            @"C:\TestSolution",
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
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        _solutionExplorer.DiscoverAndSelectProjects
        (
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<Func<string, bool>>()
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync
        ([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE
        ]);

        // Assert
        result.ShouldBe(0);
        _environment.CurrentDirectory.ShouldBe(@"C:\TestSolution");
    }

    [Fact]
    public async Task Invoke_UsesPredicateToFilterProjectsWithPackage()
    {
        // Arrange
        const string PACKAGE_ID = "TestPackage";
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string PROJECT_PATH = @"Project1\Project1.csproj";
        const string FULL_PROJECT_PATH = @"C:\TestSolution\Project1\Project1.csproj";

        _solutionExplorer.GetOrPromptSolutionFile(SOLUTION_FILE).Returns(SOLUTION_FILE);

        // Configure the predicate to be called with HasPackage check
        _solutionExplorer.DiscoverAndSelectProjects(
            SOLUTION_FILE,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Do<Func<string, bool>>(static predicate =>
            {
                // Verify the predicate calls HasPackage
                predicate(FULL_PROJECT_PATH);
            })
        ).Returns([PROJECT_PATH]);

        var rootCommand = new RootCommand { _command };

        // Act
        var result = await rootCommand.InvokeAsync([
            "rm",
            PACKAGE_ID,
            SOLUTION_FILE
        ]);

        // Assert
        result.ShouldBe(0);
        _csprojHelpers.Received().HasPackage(FULL_PROJECT_PATH, PACKAGE_ID);
    }
}
