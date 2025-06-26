using NetTools.Services;
using Spectre.Console.Testing;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using NetTools.Tests.Helpers;

namespace NetTools.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class SolutionExplorerTests
{
    private readonly TestConsole _console = new();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly SolutionExplorer _explorer;

    public SolutionExplorerTests()
    {
        _console.Interactive();
        _explorer = new SolutionExplorer(_console, _fileSystem);
    }

    [Fact]
    public void DiscoverAndSelectProjects_SolutionFileNotFound_ReturnsEmptyList()
    {
        // Arrange
        var solutionFile = "/C/NonExisten/Solution.sln".NormalizePath();
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        _fileSystem.File.Exists(solutionFile).Returns(false);

        // Act
        var result = _explorer.DiscoverAndSelectProjects(solutionFile, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain("Solution file not found or invalid.");
    }

    [Fact]
    public void DiscoverAndSelectProjects_NoProjectsFound_ReturnsEmptyListAndShowsMessage()
    {
        // Arrange
        var solutionFile = "/C/TestSolution/Solution.sln".NormalizePath();
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "Solution file not found or invalid.";

        const string SOLUTION_CONTENT = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject", "TestProject\TestProject.vbproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            """;

        _fileSystem.File.Exists(solutionFile).Returns(true);
        _fileSystem.File.ReadLines(solutionFile).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns("/C/TestSolution".NormalizePath());

        // Act
        var result = _explorer.DiscoverAndSelectProjects(solutionFile, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain(NOT_FOUND_MESSAGE);
    }

    [Fact]
    public void DiscoverAndSelectProjects_ProjectsFoundAndSelected_ReturnsSelectedProjects()
    {
        // Arrange
        var solutionFile = "/C/TestSolution/Solution.sln".NormalizePath();
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        const string SOLUTION_CONTENT = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Project1", "Project1\Project1.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Project2", "Project2\Project2.csproj", "{87654321-4321-4321-4321-210987654321}"
            EndProject
            """;

        _fileSystem.File.Exists(solutionFile).Returns(true);
        _fileSystem.File.ReadLines(solutionFile).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns("/C/TestSolution".NormalizePath());

        // Simulate user selecting the first project
        _console.Input.PushKey(ConsoleKey.Spacebar); // Select first item
        _console.Input.PushKey(ConsoleKey.Enter);    // Confirm selection

        // Act
        var result = _explorer.DiscoverAndSelectProjects(solutionFile, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain(@"Project1\Project1.csproj");
    }

    [Fact]
    public void DiscoverAndSelectProjects_WithPredicate_FiltersProjects()
    {
        // Arrange
        var solutionFile = "/C/TestSolution/Solution.sln".NormalizePath();
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        const string SOLUTION_CONTENT = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Project1", "Project1\Project1.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Project2", "Project2\Project2.csproj", "{87654321-4321-4321-4321-210987654321}"
            EndProject
            """;

        _fileSystem.File.Exists(solutionFile).Returns(true);
        _fileSystem.File.ReadLines(solutionFile).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns("/C/TestSolution".NormalizePath());

        // Predicate that excludes Project2
        static bool Predicate(string path)
            => !path.Contains("Project2");

        _console.Input.PushTextWithEnter(""); // No selection

        // Act
        _explorer.DiscoverAndSelectProjects(solutionFile, MARKUP_TITLE, NOT_FOUND_MESSAGE, Predicate);

        // Assert
        _console.Output.ShouldContain("Project1");
        _console.Output.ShouldNotContain("Project2");
    }

    [Fact]
    public void GetOrPromptSolutionFile_SolutionFileProvided_ReturnsSameFile()
    {
        // Arrange
        var solutionFile = "/C/TestSolution/Solution.sln".NormalizePath();

        // Act
        var result = _explorer.GetOrPromptSolutionFile(solutionFile);

        // Assert
        result.ShouldBe(solutionFile);
    }

    [Fact]
    public void GetOrPromptSolutionFile_NoSolutionFileInDirectory_ReturnsNull()
    {
        // Arrange
        var currentDir = "/C/TestSolution".NormalizePath();

        _fileSystem.Directory.GetCurrentDirectory().Returns(currentDir);
        _fileSystem.Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly).Returns([]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetOrPromptSolutionFile_OneSolutionFileInDirectory_ReturnsFoundFile()
    {
        // Arrange
        var currentDir = "/C/TestSolution".NormalizePath();
        var solutionFile = "/C/TestSolution/MySolution.sln".NormalizePath();

        _fileSystem.Directory.GetCurrentDirectory().Returns(currentDir);

        _fileSystem.Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns([solutionFile]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile(null);

        // Assert
        result.ShouldBe(solutionFile);
        _console.Output.ShouldContain("Found solution:");
        _console.Output.ShouldContain("MySolution.sln");
    }

    [Fact]
    public void GetOrPromptSolutionFile_MultipleSolutionFiles_PromptsUserToSelect()
    {
        // Arrange
        var currentDir = "/C/TestSolution".NormalizePath();

        string[] solutionFiles = [
            "/C/TestSolution/Solution1.sln".NormalizePath(),
            "/C/TestSolution/Solution2.sln".NormalizePath()
        ];

        _fileSystem.Directory.GetCurrentDirectory().Returns(currentDir);

        _fileSystem.Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns(solutionFiles);

        // Simulate user selecting the first solution
        _console.Input.PushKey(ConsoleKey.Enter); // Select first item (default)

        // Act
        var result = _explorer.GetOrPromptSolutionFile(null);

        // Assert
        result.ShouldBe("/C/TestSolution/Solution1.sln".NormalizePath());
        _console.Output.ShouldContain("Select the solution file:");
    }

    [Fact]
    public void GetOrPromptSolutionFile_EmptyStringProvided_SearchesInCurrentDirectory()
    {
        // Arrange
        var currentDir = "/C/TestSolution".NormalizePath();
        var solutionFile = "/C/TestSolution/MySolution.sln".NormalizePath();

        _fileSystem.Directory.GetCurrentDirectory().Returns(currentDir);

        _fileSystem.Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns([solutionFile]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile("");

        // Assert
        result.ShouldBe(solutionFile);
    }

    [Fact]
    public void GetOrPromptSolutionFile_WhitespaceProvided_SearchesInCurrentDirectory()
    {
        // Arrange
        var currentDir = "/C/TestSolution".NormalizePath();
        var solutionFile = "/C/TestSolution/MySolution.sln".NormalizePath();

        _fileSystem.Directory.GetCurrentDirectory().Returns(currentDir);

        _fileSystem.Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns([solutionFile]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile("   ");

        // Assert
        result.ShouldBe(solutionFile);
    }

    [Theory]
    [InlineData("Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestProject\", \"TestProject\\TestProject.csproj\", \"{12345678-1234-1234-1234-123456789012}\"")]
    [InlineData("PROJECT(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestProject\", \"TestProject\\TestProject.csproj\", \"{12345678-1234-1234-1234-123456789012}\"")]
    [InlineData("    Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestProject\", \"TestProject\\TestProject.csproj\", \"{12345678-1234-1234-1234-123456789012}\"")]
    public void DiscoverProjectPaths_ValidProjectLines_ExtractsProjectPaths(string projectLine)
    {
        // Arrange
        var solutionFile = "/C/TestSolution/Solution.sln".NormalizePath();
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        string solutionContent = $"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            {projectLine}
            EndProject
            """;

        _fileSystem.File.Exists(solutionFile).Returns(true);
        _fileSystem.File.ReadLines(solutionFile).Returns(solutionContent.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns("/C/TestSolution".NormalizePath());

        _console.Input.PushTextWithEnter(""); // No selection

        // Act
        _explorer.DiscoverAndSelectProjects(solutionFile, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        _console.Output.ShouldContain("TestProject/TestProject.csproj".NormalizePath());
    }

    [Fact]
    public void DiscoverProjectPaths_InvalidProjectLine_IgnoresLine()
    {
        // Arrange
        var solutionFile = "/C/TestSolution/Solution.sln".NormalizePath();
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        const string SOLUTION_CONTENT = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject"
            EndProject
            """;

        _fileSystem.File.Exists(solutionFile).Returns(true);
        _fileSystem.File.ReadLines(solutionFile).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns("/C/TestSolution".NormalizePath());

        // Act
        var result = _explorer.DiscoverAndSelectProjects(solutionFile, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain(NOT_FOUND_MESSAGE);
    }
}
