using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    /// <summary>
    /// Returns the bytes of the named embedded asset (e.g. <see cref="ScalarBundle"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The asset is not embedded in the assembly.</exception>
    public static byte[] GetBytes(string fileName)
    {
        return Cache.GetOrAdd(fileName, static name =>
        {
            var resourceName = ResolveResourceName(name);
            using var stream = OwnAssembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded asset stream '{resourceName}' could not be opened.");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        });
    }

    /// <summary>Maps a file name to its HTTP content type.</summary>
    public static string ContentTypeFor(string fileName)
    {
        if (fileName.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) return "text/javascript; charset=utf-8";
        if (fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) return "text/css; charset=utf-8";
        if (fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) return "text/html; charset=utf-8";
        return "application/octet-stream";
    }

    private static string ResolveResourceName(string fileName)
    {
        var names = OwnAssembly.GetManifestResourceNames();
        var match = Array.Find(names, n =>
            n.EndsWith(ResourceInfix + fileName, StringComparison.Ordinal));

        return match ?? throw new InvalidOperationException(
            $"Embedded asset '{fileName}' was not found. Available: {string.Join(", ", names)}");
    }
}
