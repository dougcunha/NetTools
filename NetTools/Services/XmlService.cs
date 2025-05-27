using System.Xml;
using System.Xml.Linq;

namespace NetTools.Services;

/// <summary>
/// XML service for loading and writing XML documents.
/// </summary>
public sealed class XmlService : IXmlService
{
    /// <inheritdoc />
    public XDocument Load(string uri)
        => XDocument.Load(uri);

    /// <inheritdoc />
    public XDocument Load(string uri, LoadOptions options)
        => XDocument.Load(uri, options);

    /// <inheritdoc />
    public void WriteTo(string path, string content, XmlWriterSettings settings)
    {
        using var writer = XmlWriter.Create(path, settings);
        writer.WriteRaw(content);
        writer.Flush();
    }
}
