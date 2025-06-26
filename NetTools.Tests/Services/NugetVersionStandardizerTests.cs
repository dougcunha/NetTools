using NetTools.Commands;
using NetTools.Helpers;
using NetTools.Services;
using Spectre.Console.Testing;
using System.Diagnostics.CodeAnalysis;

namespace NetTools.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class NugetVersionStandardizerTests
{
    private readonly TestConsole _console = new();
    private readonly IDotnetCommandRunner _dotnetRunner = Substitute.For<IDotnetCommandRunner>();
    private readonly ICsprojHelpers _csprojHelpers = Substitute.For<ICsprojHelpers>();
    private readonly NugetVersionStandardizer _standardizer;

    public NugetVersionStandardizerTests()
    {
        _console.Interactive();
        _standardizer = new NugetVersionStandardizer(_console, _dotnetRunner, _csprojHelpers);
    }

    [Fact]
    public void StandardizeVersions_NullSolutionFile_LogsErrorAndReturns()
    {
        // Arrange
        var options = new StandardizeCommandOptions { SolutionFile = null };

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj");

        // Assert
        _console.Output.ShouldContain("Solution file path cannot be null or empty.");
        _csprojHelpers.DidNotReceive().GetPackagesFromCsproj(Arg.Any<string>());
    }

    [Fact]
    public void StandardizeVersions_EmptySolutionFile_LogsErrorAndReturns()
    {
        // Arrange
        var options = new StandardizeCommandOptions { SolutionFile = "" };

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj");

        // Assert
        _console.Output.ShouldContain("Solution file path cannot be null or empty.");
        _csprojHelpers.DidNotReceive().GetPackagesFromCsproj(Arg.Any<string>());
    }

    [Fact]
    public void StandardizeVersions_WhitespaceSolutionFile_LogsErrorAndReturns()
    {
        // Arrange
        var options = new StandardizeCommandOptions { SolutionFile = "   " };

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj");

        // Assert
        _console.Output.ShouldContain("Solution file path cannot be null or empty.");
        _csprojHelpers.DidNotReceive().GetPackagesFromCsproj(Arg.Any<string>());
    }

    [Fact]
    public void StandardizeVersions_NoMultiVersionPackages_LogsSuccessMessage()
    {
        // Arrange
        const string SOLUTION_FILE = "/TestSolution/MySolution.sln";
        var options = new StandardizeCommandOptions { SolutionFile = SOLUTION_FILE };

        // All projects have the same package versions
        _csprojHelpers.GetPackagesFromCsproj(Arg.Any<string>())
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.0.0" });

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj", "project2.csproj");

        // Assert
        _console.Output.ShouldContain("No packages with multiple versions found.");
        _dotnetRunner.DidNotReceive().RunSequentialCommands(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>());
    }

    [Fact]
    public void StandardizeVersions_MultiVersionPackagesFound_ShowsSelectionPrompt()
    {
        // Arrange
        const string SOLUTION_FILE = "/TestSolution/MySolution.sln";
        var options = new StandardizeCommandOptions { SolutionFile = SOLUTION_FILE };

        // Setup different package versions across projects
        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project1.csproj")
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.0.0", ["PkgB"] = "2.0.0" });

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project2.csproj")
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.1.0", ["PkgB"] = "2.0.0" });

        // Mock the console prompt to return no selections (to avoid interaction)
        _console.Input.PushTextWithEnter("");

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj", "project2.csproj");

        // Assert
        _console.Output.ShouldContain("Select the packages with multiple versions to standardize:");
        _console.Output.ShouldContain("No package selected.");
    }

    [Fact]
    public void StandardizeVersions_SuccessfulStandardization_RunsDotnetCommands()
    {
        // Arrange
        const string SOLUTION_FILE = "/TestSolution/MySolution.sln";

        var options = new StandardizeCommandOptions
        {
            SolutionFile = SOLUTION_FILE,
            Clean = true,
            Restore = true,
            Build = true,
            Verbose = false
        };

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project1.csproj")
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.0.0" });

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project2.csproj")
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.1.0" });

        // Mock selection of the package
        _console.Input.PushKey(ConsoleKey.Spacebar); // Select first item
        _console.Input.PushKey(ConsoleKey.Enter);    // Confirm selection

        _dotnetRunner.RunSequentialCommands(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(true);

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj", "project2.csproj");

        // Assert
        _dotnetRunner.Received(1).RunSequentialCommands
        (
            "/TestSolution",
            "MySolution.sln",
            false,
            true,
            true,
            true
        );

        _console.Output.ShouldContain("NuGet package versions standardized and solution cleaned/restored successfully.");
    }

    [Fact]
    public void StandardizeVersions_DotnetCommandsFail_DoesNotShowSuccessMessage()
    {
        // Arrange
        const string SOLUTION_FILE = "/TestSolution/MySolution.sln";
        var options = new StandardizeCommandOptions { SolutionFile = SOLUTION_FILE };

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project1.csproj")
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.0.0" });

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project2.csproj")
            .Returns(new Dictionary<string, string> { ["PkgA"] = "1.1.0" });

        // Mock selection
        _console.Input.PushKey(ConsoleKey.Spacebar);
        _console.Input.PushKey(ConsoleKey.Enter);

        _dotnetRunner.RunSequentialCommands(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>())
            .Returns(false);

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj", "project2.csproj");

        // Assert
        _console.Output.ShouldNotContain("NuGet package versions standardized and solution cleaned/restored successfully.");
    }

    [Theory]
    [InlineData("/TestSolution/MySolution.sln", "/TestSolution")]
    [InlineData("/Projects/Test/Solution.sln", "/Projects/Test")]
    [InlineData("MySolution.sln", "")]
    public void StandardizeVersions_ExtractsSolutionDirectoryCorrectly(string solutionFile, string expectedDirectory)
    {
        // Arrange
        var options = new StandardizeCommandOptions { SolutionFile = solutionFile };

        _csprojHelpers.GetPackagesFromCsproj(Arg.Any<string>())
            .Returns(new Dictionary<string, string>());

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj");

        // Assert
        _csprojHelpers.Received().GetPackagesFromCsproj(Path.Combine(expectedDirectory, "project1.csproj"));
    }

    [Fact]
    public void StandardizeVersions_MultipleProjectsWithDifferentPackages_FindsAllMultiVersionPackages()
    {
        // Arrange
        const string SOLUTION_FILE = "/TestSolution/MySolution.sln";
        var options = new StandardizeCommandOptions { SolutionFile = SOLUTION_FILE };

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project1.csproj")
            .Returns(new Dictionary<string, string>
            {
                ["PkgA"] = "1.0.0",
                ["PkgB"] = "2.0.0",
                ["PkgC"] = "3.0.0"
            });

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project2.csproj")
            .Returns(new Dictionary<string, string>
            {
                ["PkgA"] = "1.1.0",  // Different version
                ["PkgB"] = "2.0.0",  // Same version
                ["PkgD"] = "4.0.0"   // New package
            });

        _csprojHelpers.GetPackagesFromCsproj("/TestSolution/project3.csproj")
            .Returns(new Dictionary<string, string>
            {
                ["PkgA"] = "1.2.0",  // Another different version
                ["PkgC"] = "3.1.0"   // Different version
            });

        _console.Input.PushTextWithEnter("");

        // Act
        _standardizer.StandardizeVersions(options, "project1.csproj", "project2.csproj", "project3.csproj");

        // Assert
        // Should find PkgA (1.0.0, 1.1.0, 1.2.0) and PkgC (3.0.0, 3.1.0) as multi-version packages
        _console.Output.ShouldContain("PkgA");
        _console.Output.ShouldContain("PkgC");
        _console.Output.ShouldNotContain("PkgB"); // Same version across projects
        _console.Output.ShouldNotContain("PkgD"); // Only in one project
    }
}
