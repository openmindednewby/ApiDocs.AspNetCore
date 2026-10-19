using System.Collections.Frozen;
using System.Reflection;

namespace ApiDocs.AspNetCore.Rendering;

/// <summary>
/// Loads the vendored browser assets (Scalar bundle, Swagger UI bundle/CSS, docs chrome)
/// that ship as embedded resources inside this package, so a consuming service copies
/// nothing to <c>wwwroot</c>. Bytes are read once and cached for the process lifetime.
/// </summary>
public static class EmbeddedAsset
{
    /// <summary>File name of the vendored Scalar standalone bundle.</summary>
    public const string ScalarBundle = "scalar.standalone.js";

    /// <summary>File name of the vendored Swagger UI bundle.</summary>
    public const string SwaggerUiBundle = "swagger-ui-bundle.js";

    /// <summary>File name of the vendored Swagger UI stylesheet.</summary>
    public const string SwaggerUiCss = "swagger-ui.css";

    /// <summary>File name of the shared docs chrome stylesheet.</summary>
    public const string DocsCss = "docs.css";

    /// <summary>File name of the shared docs chrome script.</summary>
    public const string DocsJs = "docs.js";

    private const string ResourceInfix = ".Assets.";

    private static readonly Assembly OwnAssembly = typeof(EmbeddedAsset).Assembly;
    private static readonly FrozenDictionary<string, Lazy<byte[]>> Assets = OwnAssembly
        .GetManifestResourceNames()
        .Where(name => name.Contains(ResourceInfix, StringComparison.Ordinal))
        .ToFrozenDictionary(
            name => name[(name.IndexOf(ResourceInfix, StringComparison.Ordinal) + ResourceInfix.Length)..],
            name => new Lazy<byte[]>(() => ReadResource(name)),
            StringComparer.Ordinal);

    /// <summary>
    /// Returns the bytes of the named embedded asset (e.g. <see cref="ScalarBundle"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The asset is not embedded in the assembly.</exception>
    public static byte[] GetBytes(string fileName)
    {
        return Assets.TryGetValue(fileName, out var asset)
            ? asset.Value
            : throw new InvalidOperationException(
                $"Embedded asset '{fileName}' was not found. Available: {string.Join(", ", OwnAssembly.GetManifestResourceNames())}");
    }

    /// <summary>Maps a file name to its HTTP content type.</summary>
    public static string ContentTypeFor(string fileName)
    {
        if (fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) return "text/javascript; charset=utf-8";
        if (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) return "text/css; charset=utf-8";
        if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) return "text/html; charset=utf-8";
        return "application/octet-stream";
    }

    private static byte[] ReadResource(string resourceName)
    {
        using var stream = OwnAssembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded asset stream '{resourceName}' could not be opened.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
