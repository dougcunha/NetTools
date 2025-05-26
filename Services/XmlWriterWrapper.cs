using System.Xml;

namespace NetTools.Services;

/// <inheritdoc/>
public class XmlWriterWrapper : IXmlWriterWrapper
{
    /// <inheritdoc/>
    public void WriteTo(string path, string content, XmlWriterSettings settings)
    {
        using var writer = XmlWriter.Create(path, settings);
        writer.WriteRaw(content);
        writer.Flush();
    }
}
