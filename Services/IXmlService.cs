using System.Xml;
using System.Xml.Linq;

namespace NetTools.Services;

/// <summary>
/// XML service for loading and writing XML documents.
/// </summary>
public interface IXmlService
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

    /// <summary>
    /// Writes the specified XML content to the given file path using the provided XmlWriterSettings.
    /// </summary>
    /// <param name="path">The file path to write the XML content to.</param>
    /// <param name="content">The XML content to write.</param>
    /// <param name="settings">The XmlWriterSettings to use.</param>
    void WriteTo(string path, string content, XmlWriterSettings settings);
}
