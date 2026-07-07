namespace ApiDocs.AspNetCore.Configuration;

/// <summary>
/// Per-service configuration for the shared API docs portal. Bind from the
/// <c>ApiDocs</c> configuration section and/or configure in code, then pass to
/// <c>app.MapApiDocs(options)</c>.
/// </summary>
public sealed class ApiDocsOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string SectionName = "ApiDocs";

    /// <summary>Default OpenAPI spec URL produced by FastEndpoints.Swagger / NSwag.</summary>
    public const string DefaultSpecUrl = "/swagger/v1/swagger.json";

    /// <summary>Default route for the branded Scalar reference.</summary>
    public const string DefaultReferenceRoute = "/docs";

    /// <summary>
    /// Default route for the raw Swagger UI. Intentionally NOT <c>/swagger</c> so it does
    /// not collide with a service that already serves NSwag's UI there.
    /// </summary>
    public const string DefaultSwaggerUiRoute = "/swagger-ui";

    /// <summary>Default accent colour (indigo).</summary>
    public const string DefaultBrandColor = "#4f46e5";

    /// <summary>Default secondary accent colour (teal).</summary>
    public const string DefaultBrandColorSecondary = "#0fb5a6";

    /// <summary>Default attribution text.</summary>
    public const string DefaultAttribution = "dloizides.com";

    /// <summary>Default attribution URL.</summary>
    public const string DefaultAttributionUrl = "https://dloizides.com";

    // ---- Identity / branding ----

    /// <summary>Page title and default brand name, e.g. "Kefi API".</summary>
    public string Title { get; set; } = "API";

    /// <summary>
    /// Sidebar brand name. Defaults to <see cref="Title"/> when left null/empty.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>Short uppercase eyebrow above the brand name, e.g. "Developer docs".</summary>
    public string Eyebrow { get; set; } = "Developer docs";

    /// <summary>One-line description rendered under the reference header.</summary>
    public string Description { get; set; } = "Live, interactive OpenAPI reference — always in sync with the running service.";

    /// <summary>
    /// Accent colour (hex). Injected as the <c>--accent</c> CSS variable so the whole
    /// chrome themes to the service brand.
    /// </summary>
    public string BrandColor { get; set; } = DefaultBrandColor;

    /// <summary>Secondary accent colour (hex), injected as <c>--accent-2</c>.</summary>
    public string BrandColorSecondary { get; set; } = DefaultBrandColorSecondary;

    /// <summary>
    /// Optional inline SVG markup for the sidebar/header brand mark. When null/empty a
    /// lettered mark derived from <see cref="ServiceName"/>/<see cref="Title"/> is used.
    /// Must be self-contained SVG (no external references).
    /// </summary>
    public string? LogoSvg { get; set; }

    // ---- Routes / spec ----

    /// <summary>OpenAPI spec URL both renderers read from.</summary>
    public string SpecUrl { get; set; } = DefaultSpecUrl;

    /// <summary>Route prefix for the branded Scalar reference (and its assets).</summary>
    public string ReferenceRoute { get; set; } = DefaultReferenceRoute;

    /// <summary>Route prefix for the raw Swagger UI (and its assets).</summary>
    public string SwaggerUiRoute { get; set; } = DefaultSwaggerUiRoute;

    /// <summary>When true (default), maps the branded Scalar reference.</summary>
    public bool EnableReference { get; set; } = true;

    /// <summary>When true (default), maps the raw Swagger UI (the owner-requested "swagger too").</summary>
    public bool EnableSwaggerUi { get; set; } = true;

    /// <summary>
    /// When true (default), includes the Authentication / Errors / Rate limits deep-dive
    /// pages in the sidebar and routes.
    /// </summary>
    public bool IncludeDeepDivePages { get; set; } = true;

    // ---- Scalar tuning ----

    /// <summary>Scalar theme name (e.g. "default", "moon", "purple").</summary>
    public string ScalarTheme { get; set; } = "default";

    /// <summary>When true, Scalar renders in dark mode.</summary>
    public bool ScalarDarkMode { get; set; }

    // ---- Attribution ----

    /// <summary>Discreet "built by" attribution text.</summary>
    public string Attribution { get; set; } = DefaultAttribution;

    /// <summary>Attribution URL (used only when <see cref="AttributionAsLink"/> is true).</summary>
    public string AttributionUrl { get; set; } = DefaultAttributionUrl;

    /// <summary>
    /// When false (default) the attribution is rendered as plain text, keeping the served
    /// HTML free of any external absolute URL (CSP-clean). Set true to render it as a link.
    /// </summary>
    public bool AttributionAsLink { get; set; }

    // ---- Auth gating ----

    /// <summary>How the docs endpoints are gated. Defaults to <see cref="ApiDocsAuthMode.RequireAuthInProduction"/>.</summary>
    public ApiDocsAuthMode AuthMode { get; set; } = ApiDocsAuthMode.RequireAuthInProduction;

    /// <summary>
    /// Optional named authorization policy applied when auth is required. When null, the
    /// default authorization policy is used.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    // ---- Extra links ----

    /// <summary>Extra sidebar/header links (guides, status page, changelog, ...).</summary>
    public IList<DocsNavLink> NavLinks { get; set; } = new List<DocsNavLink>();

    /// <summary>Resolves the effective sidebar brand name.</summary>
    public string EffectiveServiceName =>
        string.IsNullOrWhiteSpace(ServiceName) ? Title : ServiceName!;
}
