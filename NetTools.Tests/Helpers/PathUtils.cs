namespace NetTools.Tests.Helpers;

public static class PathUtils
{
    /// <summary>
    /// Normalizes a file path by replacing backslashes and forward slashes with the system's directory separator character.
    /// It ensures that the path is consistent with the current operating system's file path conventions.
    /// </summary>
    /// <param name="path">
    /// A string representing the file path to be normalized.
    /// </param>
    /// <returns>
    /// A normalized file path string where all backslashes and forward slashes are replaced with the system's directory separator character.
    /// </returns>
    public static string NormalizePath(this string path) =>
        path.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
}
