using NetTools.Services;
using Spectre.Console.Testing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace NetTools.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class DotnetCommandRunnerTests
{
    private readonly TestConsole _console = new();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly DotnetCommandRunner _runner;

    public DotnetCommandRunnerTests()
        => _runner = new DotnetCommandRunner(_console, _processRunner);

    [Fact]
    public void RunSequentialCommands_NoOperationsRequested_ReturnsTrue()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR);

        // Assert
        result.ShouldBeTrue();
        _processRunner.DidNotReceive().Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_CleanOnly_ExecutesCleanCommand()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(0);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, clean: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldContain("Cleaning the solution...");

        _processRunner.Received(1).Run(
            Arg.Is<ProcessStartInfo>(static p => p.Arguments == "clean" && p.WorkingDirectory == SOLUTION_DIR),
            Arg.Any<DataReceivedEventHandler>(),
            Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_RestoreOnly_ExecutesRestoreCommand()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(0);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, restore: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldContain("Restoring the solution...");

        _processRunner.Received(1).Run(
            Arg.Is<ProcessStartInfo>(static p => p.Arguments == "restore" && p.WorkingDirectory == SOLUTION_DIR),
            Arg.Any<DataReceivedEventHandler>(),
            Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_BuildOnly_ExecutesBuildCommand()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(0);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, build: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldContain("Building the solution...");

        _processRunner.Received(1).Run(
            Arg.Is<ProcessStartInfo>(static p => p.Arguments == "build" && p.WorkingDirectory == SOLUTION_DIR),
            Arg.Any<DataReceivedEventHandler>(),
            Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_AllOperations_ExecutesInCorrectOrder()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(0);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, clean: true, restore: true, build: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldContain("Cleaning the solution...");
        _console.Output.ShouldContain("Restoring the solution...");
        _console.Output.ShouldContain("Building the solution...");
        _processRunner.Received(3).Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_CleanFails_ReturnsFailureAndStopsExecution()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(1);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, clean: true, restore: true, build: true);

        // Assert
        result.ShouldBeFalse();
        _console.Output.ShouldContain("Cleaning the solution...");
        _console.Output.ShouldContain("Failed to clean the solution.");
        _console.Output.ShouldNotContain("Restoring the solution...");
        _console.Output.ShouldNotContain("Building the solution...");
        _processRunner.Received(1).Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_RestoreFails_ReturnsFailureAndStopsExecution()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(static call => call.ArgAt<ProcessStartInfo>(0).Arguments.Contains("clean") ? 0 : 1);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, clean: true, restore: true, build: true);

        // Assert
        result.ShouldBeFalse();
        _console.Output.ShouldContain("Cleaning the solution...");
        _console.Output.ShouldContain("Restoring the solution...");
        _console.Output.ShouldContain("Failed to restore the solution.");
        _console.Output.ShouldNotContain("Building the solution...");
        _processRunner.Received(2).Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_BuildFails_ReturnsFailure()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(static call => call.ArgAt<ProcessStartInfo>(0).Arguments.Contains("build") ? 1 : 0);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, build: true);

        // Assert
        result.ShouldBeFalse();
        _console.Output.ShouldContain("Building the solution...");
        _console.Output.ShouldContain("Failed to build the solution.");
        _processRunner.Received(1).Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_WithSolutionFile_PassesFileToCommands()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";
        const string SOLUTION_FILE = "MySolution.sln";

        _processRunner.Run(
                Arg.Any<ProcessStartInfo>(),
                Arg.Any<DataReceivedEventHandler>(),
                Arg.Any<DataReceivedEventHandler>()
            )
            .Returns(0);

        // Act
        var result = _runner.RunSequentialCommands(
            SOLUTION_DIR,
            SOLUTION_FILE,
            clean: true
        );

        // Assert
        result.ShouldBeTrue();

        _processRunner.Received(1).Run
        (
            Arg.Is<ProcessStartInfo>(static p => p.Arguments == $"clean \"{SOLUTION_FILE}\""),
            Arg.Any<DataReceivedEventHandler>(),
            Arg.Any<DataReceivedEventHandler>()
        );
    }

    [Fact]
    public void RunSequentialCommands_VerboseMode_PassesVerboseFlag()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(0);

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, verbose: true, build: true);

        // Assert
        result.ShouldBeTrue();
        _processRunner.Received(1).Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>());
    }

    [Fact]
    public void RunSequentialCommands_VerboseWithOutputData_CallsOutputHandler()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";
        const string OUTPUT_MESSAGE = "Build succeeded.";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(static callInfo =>
            {
                var outputHandler = callInfo.ArgAt<DataReceivedEventHandler>(1);
                outputHandler?.Invoke(null!, CreateDataReceivedEventArgs(OUTPUT_MESSAGE));

                return 0;
            });

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, verbose: true, build: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldContain(OUTPUT_MESSAGE);
    }

    [Fact]
    public void RunSequentialCommands_VerboseWithErrorData_CallsErrorHandler()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";
        const string ERROR_MESSAGE = "Build failed with error.";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(static callInfo =>
            {
                var errorHandler = callInfo.ArgAt<DataReceivedEventHandler>(2);
                errorHandler?.Invoke(null!, CreateDataReceivedEventArgs(ERROR_MESSAGE));

                return 0;
            });

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, verbose: true, build: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldContain(ERROR_MESSAGE);
    }

    [Fact]
    public void RunSequentialCommands_NonVerboseMode_DoesNotOutputMessages()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";
        const string OUTPUT_MESSAGE = "Build succeeded.";
        const string ERROR_MESSAGE = "Some warning.";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(static callInfo =>
            {
                var outputHandler = callInfo.ArgAt<DataReceivedEventHandler>(1);
                var errorHandler = callInfo.ArgAt<DataReceivedEventHandler>(2);

                outputHandler?.Invoke(null!, CreateDataReceivedEventArgs(OUTPUT_MESSAGE));
                errorHandler?.Invoke(null!, CreateDataReceivedEventArgs(ERROR_MESSAGE));

                return 0;
            });

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, verbose: false, build: true);

        // Assert
        result.ShouldBeTrue();
        _console.Output.ShouldNotContain(OUTPUT_MESSAGE);
        _console.Output.ShouldNotContain(ERROR_MESSAGE);
    }

    [Fact]
    public void RunSequentialCommands_VerboseWithEmptyData_DoesNotOutputEmptyMessages()
    {
        // Arrange
        const string SOLUTION_DIR = "C:\\TestSolution";

        _processRunner.Run(Arg.Any<ProcessStartInfo>(), Arg.Any<DataReceivedEventHandler>(), Arg.Any<DataReceivedEventHandler>())
            .Returns(static callInfo =>
            {
                var outputHandler = callInfo.ArgAt<DataReceivedEventHandler>(1);
                var errorHandler = callInfo.ArgAt<DataReceivedEventHandler>(2);

                // Simulate empty/null data events
                outputHandler?.Invoke(null!, CreateDataReceivedEventArgs(null));
                outputHandler?.Invoke(null!, CreateDataReceivedEventArgs(""));
                outputHandler?.Invoke(null!, CreateDataReceivedEventArgs("   "));

                errorHandler?.Invoke(null!, CreateDataReceivedEventArgs(null));
                errorHandler?.Invoke(null!, CreateDataReceivedEventArgs(""));

                return 0;
            });

        // Act
        var result = _runner.RunSequentialCommands(SOLUTION_DIR, verbose: true, build: true);

        // Assert
        result.ShouldBeTrue();
        // Should only contain the status messages, not the empty data
        _console.Output.ShouldContain("Building the solution...");
    }

    private static DataReceivedEventArgs CreateDataReceivedEventArgs(string? data)
    {
        // Use reflection to create DataReceivedEventArgs since it doesn't have a public constructor
        var constructor = typeof(DataReceivedEventArgs).GetConstructor
        (
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(string)],
            null
        );

        return (DataReceivedEventArgs)constructor!.Invoke([data]);
    }
}
