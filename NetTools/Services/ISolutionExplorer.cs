
namespace NetTools.Services;

public interface ISolutionExplorer
{
    List<string> DiscoverAndSelectProjects(string? solutionFile, string markupTitle, string notFoundMessage, Func<string, bool>? predicate = null);
    string? GetOrPromptSolutionFile(string? solutionFile);
}