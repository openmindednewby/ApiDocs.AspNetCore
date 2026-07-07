using System.Text;
using ApiDocs.AspNetCore.Rendering;
using Shouldly;

namespace ApiDocs.AspNetCore.Tests.Rendering;

/// <summary>
/// Verifies the vendored browser assets are actually embedded in the assembly and are
/// self-contained (no CDN references baked into the shipped chrome).
/// </summary>
public class EmbeddedAssetTests
{
    [Theory]
    [InlineData(EmbeddedAsset.ScalarBundle, 1_000_000)]
    [InlineData(EmbeddedAsset.SwaggerUiBundle, 500_000)]
    [InlineData(EmbeddedAsset.SwaggerUiCss, 50_000)]
    [InlineData(EmbeddedAsset.DocsCss, 3_000)]
    [InlineData(EmbeddedAsset.DocsJs, 500)]
    public void GetBytes_ForVendoredAsset_ReturnsNonTrivialContent(string fileName, int minBytes)
    {
        // Act
        var bytes = EmbeddedAsset.GetBytes(fileName);

        // Assert
        bytes.Length.ShouldBeGreaterThan(minBytes);
    }

    [Fact]
    public void GetBytes_ForScalarBundle_IsTheScalarApiReference()
    {
        // Act
        var text = Encoding.UTF8.GetString(EmbeddedAsset.GetBytes(EmbeddedAsset.ScalarBundle));

        // Assert
        text.ShouldContain("@scalar/api-reference");
    }

    [Fact]
    public void GetBytes_ForDocsCss_HasNoExternalImportOrCdn()
    {
        // Act — the AML original imported /shared/ui.css and would have been service-coupled;
        // the packaged chrome must be standalone.
        var text = Encoding.UTF8.GetString(EmbeddedAsset.GetBytes(EmbeddedAsset.DocsCss));

        // Assert
        text.ShouldNotContain("@import");
        text.ShouldNotContain("cdn.");
        text.ShouldNotContain("://");
    }

    [Fact]
    public void GetBytes_ForSwaggerUiCss_ReferencesNoExternalUrl()
    {
        // Act
        var text = Encoding.UTF8.GetString(EmbeddedAsset.GetBytes(EmbeddedAsset.SwaggerUiCss));

        // Assert — all images are inlined as data: URIs.
        text.ShouldNotContain("url(https://");
        text.ShouldNotContain("url(http://");
    }

    [Theory]
    [InlineData(EmbeddedAsset.ScalarBundle, "text/javascript; charset=utf-8")]
    [InlineData(EmbeddedAsset.SwaggerUiCss, "text/css; charset=utf-8")]
    public void ContentTypeFor_ForKnownExtensions_ReturnsExpected(string fileName, string expected)
    {
        // Act & Assert
        EmbeddedAsset.ContentTypeFor(fileName).ShouldBe(expected);
    }

    [Fact]
    public void GetBytes_ForUnknownAsset_Throws()
    {
        // Act
        var act = () => EmbeddedAsset.GetBytes("does-not-exist.js");

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}
