using ApiDocs.AspNetCore.Configuration;
using ApiDocs.AspNetCore.Rendering;
using Shouldly;

namespace ApiDocs.AspNetCore.Tests.Rendering;

/// <summary>
/// Unit tests for <see cref="DocsPageRenderer"/> — the branded, self-contained HTML output.
/// </summary>
public class DocsPageRendererTests
{
    private static ApiDocsOptions SampleOptions() => new()
    {
        Title = "Kefi API",
        ServiceName = "Kefi",
        BrandColor = "#7c3aed",
        BrandColorSecondary = "#14b8a6",
        SpecUrl = "/swagger/v1/swagger.json",
    };

    [Fact]
    public void RenderReferencePage_Always_ReferencesVendoredScalarAsset()
    {
        // Arrange
        var options = SampleOptions();

        // Act
        var html = DocsPageRenderer.RenderReferencePage(options);

        // Assert
        html.ShouldContain("/docs/assets/scalar.standalone.js");
        html.ShouldContain("createApiReference");
    }

    [Fact]
    public void RenderReferencePage_Always_ContainsNoExternalHostOrCdn()
    {
        // Arrange
        var options = SampleOptions();

        // Act
        var html = DocsPageRenderer.RenderReferencePage(options);

        // Assert — CSP-safe: no external absolute URL, no CDN reference.
        html.ShouldNotContain("cdn.");
        html.ShouldNotContain("://");
    }

    [Fact]
    public void RenderReferencePage_Always_DisablesScalarExternalFontsAndProxy()
    {
        // Arrange
        var options = SampleOptions();

        // Act
        var html = DocsPageRenderer.RenderReferencePage(options);

        // Assert — the two runtime calls Scalar would otherwise make to fonts.scalar.com /
        // proxy.scalar.com are neutralized.
        html.ShouldContain("\"withDefaultFonts\":false");
        html.ShouldContain("\"proxyUrl\":\"\"");
    }

    [Fact]
    public void RenderReferencePage_WithBrandColor_InjectsAccentCssVariable()
    {
        // Arrange
        var options = SampleOptions();

        // Act
        var html = DocsPageRenderer.RenderReferencePage(options);

        // Assert
        html.ShouldContain("--accent:#7c3aed");
        html.ShouldContain("Kefi");
    }

    [Fact]
    public void RenderReferencePage_WhenBrandColorIsMalicious_FallsBackToDefault()
    {
        // Arrange — a CSS/HTML-injection attempt in the colour must not survive.
        var options = SampleOptions();
        options.BrandColor = "red;}</style><script>alert(1)</script>";

        // Act
        var html = DocsPageRenderer.RenderReferencePage(options);

        // Assert
        html.ShouldContain($"--accent:{ApiDocsOptions.DefaultBrandColor}");
        html.ShouldNotContain("<script>alert(1)</script>");
    }

    [Fact]
    public void RenderSwaggerUiPage_Always_ReferencesVendoredSwaggerAssetsAndDisablesValidator()
    {
        // Arrange
        var options = SampleOptions();

        // Act
        var html = DocsPageRenderer.RenderSwaggerUiPage(options);

        // Assert
        html.ShouldContain("/swagger-ui/assets/swagger-ui-bundle.js");
        html.ShouldContain("/swagger-ui/assets/swagger-ui.css");
        html.ShouldContain("validatorUrl:null");
        html.ShouldNotContain("://");
    }

    [Theory]
    [InlineData(DocsPageRenderer.AuthenticationSlug, "Authentication")]
    [InlineData(DocsPageRenderer.ErrorsSlug, "Errors")]
    [InlineData(DocsPageRenderer.RateLimitsSlug, "Rate limits")]
    public void RenderDeepDivePage_ForKnownSlug_RendersHeadingCspSafe(string slug, string expectedHeading)
    {
        // Arrange
        var options = SampleOptions();

        // Act
        var html = DocsPageRenderer.RenderDeepDivePage(options, slug);

        // Assert
        html.ShouldContain(expectedHeading);
        html.ShouldContain("/docs/assets/docs.css");
        html.ShouldNotContain("://");
    }

    [Fact]
    public void RenderReferencePage_WhenAttributionAsLinkTrue_EmitsExternalAnchorByChoice()
    {
        // Arrange — opting into a real link is the ONLY way an absolute URL appears.
        var options = SampleOptions();
        options.AttributionAsLink = true;

        // Act — deep-dive footer is where attribution renders as a link.
        var html = DocsPageRenderer.RenderDeepDivePage(options, DocsPageRenderer.AuthenticationSlug);

        // Assert
        html.ShouldContain(ApiDocsOptions.DefaultAttributionUrl);
    }

    [Theory]
    [InlineData("/docs/", "/docs")]
    [InlineData("docs", "/docs")]
    [InlineData("", "/")]
    [InlineData("/docs", "/docs")]
    public void NormalizeRoute_ForVariousInputs_ProducesLeadingSlashNoTrailing(string input, string expected)
    {
        // Act
        var result = DocsPageRenderer.NormalizeRoute(input);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void IsKnownDeepDiveSlug_ForUnknownSlug_ReturnsFalse()
    {
        // Act & Assert
        DocsPageRenderer.IsKnownDeepDiveSlug("nonsense").ShouldBeFalse();
        DocsPageRenderer.IsKnownDeepDiveSlug(DocsPageRenderer.ErrorsSlug).ShouldBeTrue();
    }

    [Fact]
    public void RenderReferencePage_WithCustomRoutes_UsesConfiguredPrefixes()
    {
        // Arrange
        var options = SampleOptions();
        options.ReferenceRoute = "/api-docs";
        options.SwaggerUiRoute = "/swagger";

        // Act
        var html = DocsPageRenderer.RenderReferencePage(options);

        // Assert
        html.ShouldContain("/api-docs/assets/scalar.standalone.js");
        html.ShouldContain("/swagger");
    }
}
