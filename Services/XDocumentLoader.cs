using System.Xml.Linq;

namespace NetTools.Services;

/// <inheritdoc/>
public class XDocumentLoader : IXDocumentLoader
{
    /// <inheritdoc/>
    public XDocument Load(string uri)
        => XDocument.Load(uri);

    public XDocument Load(string uri, LoadOptions options)
        => XDocument.Load(uri, options);
}
