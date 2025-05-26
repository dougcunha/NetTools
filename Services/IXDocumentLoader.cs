using System.Xml.Linq;

namespace NetTools.Services;

/// <summary>
/// Interface for loading XML documents.
/// </summary>
public interface IXDocumentLoader
{
    /// <summary>
    /// Loads an XML document from the specified URI.
    /// </summary>
    /// <param name="uri">The URI of the XML document to load.</param>
    /// <returns>An <see cref="XDocument"/> representing the loaded XML document.</returns>
    XDocument Load(string uri);

    /// <summary>
    /// Loads an XML document from the specified URI with the given LoadOptions.
    /// </summary>
    /// <param name="uri">The URI of the XML document to load.</param>
    /// <param name="options">The options for loading the XML document.</param>
    /// <returns>An <see cref="XDocument"/> representing the loaded XML document.</returns>
    XDocument Load(string uri, LoadOptions options);
}
