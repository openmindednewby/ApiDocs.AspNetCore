namespace ApiDocs.AspNetCore.Configuration;

/// <summary>
/// An extra link rendered in the docs sidebar / reference header, letting a service
/// point at its own guides, status page, changelog, etc.
/// </summary>
public sealed class DocsNavLink
{
    /// <summary>Visible link text.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Link target. Prefer a same-origin relative path (e.g. <c>/status</c>) to keep the
    /// page CSP-clean; absolute external URLs are allowed but are anchor links only.
    /// </summary>
    public string Href { get; set; } = string.Empty;

    /// <summary>When true, opens the link in a new tab.</summary>
    public bool NewTab { get; set; }
}
