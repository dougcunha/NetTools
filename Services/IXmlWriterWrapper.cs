using System.Xml;

namespace NetTools.Services;

/// <summary>
/// Interface for writing XML content to a file using XmlWriter.
/// </summary>
public interface IXmlWriterWrapper
{
    /// <summary>
    /// Writes the specified XML content to the given file path using the provided XmlWriterSettings.
    /// </summary>
    /// <param name="path">The file path to write the XML content to.</param>
    /// <param name="content">The XML content to write.</param>
    /// <param name="settings">The XmlWriterSettings to use.</param>
    void WriteTo(string path, string content, XmlWriterSettings settings);
}
