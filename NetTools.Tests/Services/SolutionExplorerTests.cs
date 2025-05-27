using NetTools.Services;
using Spectre.Console.Testing;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

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
        const string SOLUTION_FILE = @"C:\NonExistent\Solution.sln";
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        _fileSystem.File.Exists(SOLUTION_FILE).Returns(false);

        // Act
        var result = _explorer.DiscoverAndSelectProjects(SOLUTION_FILE, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain("Solution file not found or invalid.");
    }

    [Fact]
    public void DiscoverAndSelectProjects_NoProjectsFound_ReturnsEmptyListAndShowsMessage()
    {
        // Arrange
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "Solution file not found or invalid.";

        const string SOLUTION_CONTENT = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject", "TestProject\TestProject.vbproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            """;

        _fileSystem.File.Exists(SOLUTION_FILE).Returns(true);
        _fileSystem.File.ReadLines(SOLUTION_FILE).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns(@"C:\TestSolution");

        // Act
        var result = _explorer.DiscoverAndSelectProjects(SOLUTION_FILE, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain(NOT_FOUND_MESSAGE);
    }

    [Fact]
    public void DiscoverAndSelectProjects_ProjectsFoundAndSelected_ReturnsSelectedProjects()
    {
        // Arrange
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
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

        _fileSystem.File.Exists(SOLUTION_FILE).Returns(true);
        _fileSystem.File.ReadLines(SOLUTION_FILE).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns(@"C:\TestSolution");

        // Simulate user selecting the first project
        _console.Input.PushKey(ConsoleKey.Spacebar); // Select first item
        _console.Input.PushKey(ConsoleKey.Enter);    // Confirm selection

        // Act
        var result = _explorer.DiscoverAndSelectProjects(SOLUTION_FILE, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldNotBeEmpty();
        result.ShouldContain(@"Project1\Project1.csproj");
    }

    [Fact]
    public void DiscoverAndSelectProjects_WithPredicate_FiltersProjects()
    {
        // Arrange
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
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

        _fileSystem.File.Exists(SOLUTION_FILE).Returns(true);
        _fileSystem.File.ReadLines(SOLUTION_FILE).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns(@"C:\TestSolution");

        // Predicate that excludes Project2
        static bool Predicate(string path)
            => !path.Contains("Project2");

        _console.Input.PushTextWithEnter(""); // No selection

        // Act
        _explorer.DiscoverAndSelectProjects(SOLUTION_FILE, MARKUP_TITLE, NOT_FOUND_MESSAGE, Predicate);

        // Assert
        _console.Output.ShouldContain("Project1");
        _console.Output.ShouldNotContain("Project2");
    }

    [Fact]
    public void GetOrPromptSolutionFile_SolutionFileProvided_ReturnsSameFile()
    {
        // Arrange
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";

        // Act
        var result = _explorer.GetOrPromptSolutionFile(SOLUTION_FILE);

        // Assert
        result.ShouldBe(SOLUTION_FILE);
    }

    [Fact]
    public void GetOrPromptSolutionFile_NoSolutionFileInDirectory_ReturnsNull()
    {
        // Arrange
        const string CURRENT_DIR = @"C:\TestSolution";

        _fileSystem.Directory.GetCurrentDirectory().Returns(CURRENT_DIR);
        _fileSystem.Directory.GetFiles(CURRENT_DIR, "*.sln", SearchOption.TopDirectoryOnly).Returns([]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetOrPromptSolutionFile_OneSolutionFileInDirectory_ReturnsFoundFile()
    {
        // Arrange
        const string CURRENT_DIR = @"C:\TestSolution";
        const string SOLUTION_FILE = @"C:\TestSolution\MySolution.sln";

        _fileSystem.Directory.GetCurrentDirectory().Returns(CURRENT_DIR);

        _fileSystem.Directory.GetFiles(CURRENT_DIR, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns([SOLUTION_FILE]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile(null);

        // Assert
        result.ShouldBe(SOLUTION_FILE);
        _console.Output.ShouldContain("Found solution:");
        _console.Output.ShouldContain("MySolution.sln");
    }

    [Fact]
    public void GetOrPromptSolutionFile_MultipleSolutionFiles_PromptsUserToSelect()
    {
        // Arrange
        const string CURRENT_DIR = @"C:\TestSolution";

        string[] solutionFiles = [
            @"C:\TestSolution\Solution1.sln",
            @"C:\TestSolution\Solution2.sln"
        ];

        _fileSystem.Directory.GetCurrentDirectory().Returns(CURRENT_DIR);

        _fileSystem.Directory.GetFiles(CURRENT_DIR, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns(solutionFiles);

        // Simulate user selecting the first solution
        _console.Input.PushKey(ConsoleKey.Enter); // Select first item (default)

        // Act
        var result = _explorer.GetOrPromptSolutionFile(null);

        // Assert
        result.ShouldBe(@"C:\TestSolution\Solution1.sln");
        _console.Output.ShouldContain("Select the solution file:");
    }

    [Fact]
    public void GetOrPromptSolutionFile_EmptyStringProvided_SearchesInCurrentDirectory()
    {
        // Arrange
        const string CURRENT_DIR = @"C:\TestSolution";
        const string SOLUTION_FILE = @"C:\TestSolution\MySolution.sln";

        _fileSystem.Directory.GetCurrentDirectory().Returns(CURRENT_DIR);

        _fileSystem.Directory.GetFiles(CURRENT_DIR, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns([SOLUTION_FILE]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile("");

        // Assert
        result.ShouldBe(SOLUTION_FILE);
    }

    [Fact]
    public void GetOrPromptSolutionFile_WhitespaceProvided_SearchesInCurrentDirectory()
    {
        // Arrange
        const string CURRENT_DIR = @"C:\TestSolution";
        const string SOLUTION_FILE = @"C:\TestSolution\MySolution.sln";

        _fileSystem.Directory.GetCurrentDirectory().Returns(CURRENT_DIR);

        _fileSystem.Directory.GetFiles(CURRENT_DIR, "*.sln", SearchOption.TopDirectoryOnly)
            .Returns([SOLUTION_FILE]);

        // Act
        var result = _explorer.GetOrPromptSolutionFile("   ");

        // Assert
        result.ShouldBe(SOLUTION_FILE);
    }

    [Theory]
    [InlineData("Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestProject\", \"TestProject\\TestProject.csproj\", \"{12345678-1234-1234-1234-123456789012}\"")]
    [InlineData("PROJECT(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestProject\", \"TestProject\\TestProject.csproj\", \"{12345678-1234-1234-1234-123456789012}\"")]
    [InlineData("    Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"TestProject\", \"TestProject\\TestProject.csproj\", \"{12345678-1234-1234-1234-123456789012}\"")]
    public void DiscoverProjectPaths_ValidProjectLines_ExtractsProjectPaths(string projectLine)
    {
        // Arrange
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        string solutionContent = $"""
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            {projectLine}
            EndProject
            """;

        _fileSystem.File.Exists(SOLUTION_FILE).Returns(true);
        _fileSystem.File.ReadLines(SOLUTION_FILE).Returns(solutionContent.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns(@"C:\TestSolution");

        _console.Input.PushTextWithEnter(""); // No selection

        // Act
        _explorer.DiscoverAndSelectProjects(SOLUTION_FILE, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        _console.Output.ShouldContain(@"TestProject\TestProject.csproj");
    }

    [Fact]
    public void DiscoverProjectPaths_InvalidProjectLine_IgnoresLine()
    {
        // Arrange
        const string SOLUTION_FILE = @"C:\TestSolution\Solution.sln";
        const string MARKUP_TITLE = "Select projects";
        const string NOT_FOUND_MESSAGE = "No projects found";

        const string SOLUTION_CONTENT = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject"
            EndProject
            """;

        _fileSystem.File.Exists(SOLUTION_FILE).Returns(true);
        _fileSystem.File.ReadLines(SOLUTION_FILE).Returns(SOLUTION_CONTENT.Split('\n'));
        _fileSystem.Directory.GetCurrentDirectory().Returns(@"C:\TestSolution");

        // Act
        var result = _explorer.DiscoverAndSelectProjects(SOLUTION_FILE, MARKUP_TITLE, NOT_FOUND_MESSAGE);

        // Assert
        result.ShouldBeEmpty();
        _console.Output.ShouldContain(NOT_FOUND_MESSAGE);
    }
}
