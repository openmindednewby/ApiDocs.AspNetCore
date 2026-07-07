using System.Net;
using System.Text;
using System.Text.Json;
using ApiDocs.AspNetCore.Configuration;

namespace ApiDocs.AspNetCore.Rendering;

/// <summary>
/// Builds the branded, fully self-contained docs HTML for a service. Every asset it
/// references is served same-origin from this package's endpoints — the output contains
/// no external absolute URL by default, keeping it CSP-safe.
/// </summary>
public static class DocsPageRenderer
{
    /// <summary>Slug of the Authentication deep-dive page.</summary>
    public const string AuthenticationSlug = "authentication";

    /// <summary>Slug of the Errors deep-dive page.</summary>
    public const string ErrorsSlug = "errors";

    /// <summary>Slug of the Rate limits deep-dive page.</summary>
    public const string RateLimitsSlug = "rate-limits";

    private static readonly JsonSerializerOptions ScalarJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Renders the branded Scalar interactive reference (the primary page).</summary>
    public static string RenderReferencePage(ApiDocsOptions options)
    {
        var refRoute = NormalizeRoute(options.ReferenceRoute);
        var scalarSrc = refRoute + "/assets/" + EmbeddedAsset.ScalarBundle;
        var cssHref = refRoute + "/assets/" + EmbeddedAsset.DocsCss;
        var config = JsonSerializer.Serialize(new
        {
            url = options.SpecUrl,
            theme = options.ScalarTheme,
            layout = "modern",
            hideDownloadButton = false,
            darkMode = options.ScalarDarkMode,
            withDefaultFonts = false, // do NOT fetch fonts.scalar.com
            proxyUrl = string.Empty,  // spec is same-origin; never use proxy.scalar.com
        }, ScalarJson);

        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\" />\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n");
        sb.Append($"<title>{Enc(options.Title)} — API reference</title>\n");
        sb.Append($"<meta name=\"description\" content=\"{Enc(options.Description)}\" />\n");
        sb.Append($"<link rel=\"stylesheet\" href=\"{Enc(cssHref)}\" />\n");
        sb.Append(BrandStyle(options));
        sb.Append("<style>html,body{margin:0;height:100%;background:var(--surface,#fff);}#ref-app{width:100%;min-height:70vh;}</style>\n");
        sb.Append("</head>\n<body>\n");
        sb.Append("<a class=\"skip-link\" href=\"#ref-app\">Skip to the API reference</a>\n");
        sb.Append(ReferenceHeader(options, refRoute));
        sb.Append("<main id=\"ref-app\">\n");
        sb.Append("<noscript><div class=\"ref-fallback\">This interactive reference needs JavaScript. The raw spec is at ");
        sb.Append($"<a href=\"{Enc(options.SpecUrl)}\">{Enc(options.SpecUrl)}</a>.</div></noscript>\n");
        sb.Append("<div id=\"ref-loading\" class=\"ref-fallback\">Loading the live API reference…</div>\n");
        sb.Append("</main>\n");
        sb.Append($"<script src=\"{Enc(scalarSrc)}\"></script>\n");
        sb.Append(ReferenceInitScript(config, options.SpecUrl, options));
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    /// <summary>Renders the raw Swagger UI page over the same spec.</summary>
    public static string RenderSwaggerUiPage(ApiDocsOptions options)
    {
        var uiRoute = NormalizeRoute(options.SwaggerUiRoute);
        var bundleSrc = uiRoute + "/assets/" + EmbeddedAsset.SwaggerUiBundle;
        var cssHref = uiRoute + "/assets/" + EmbeddedAsset.SwaggerUiCss;
        var init = new StringBuilder();
        init.Append("window.ui = SwaggerUIBundle({");
        init.Append($"url:{JsStr(options.SpecUrl)},");
        init.Append("dom_id:'#swagger-ui',");
        init.Append("deepLinking:true,");
        init.Append("validatorUrl:null,"); // do NOT call validator.swagger.io
        init.Append("presets:[SwaggerUIBundle.presets.apis],");
        init.Append("layout:'BaseLayout'});");

        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\" />\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n");
        sb.Append($"<title>{Enc(options.Title)} — Swagger UI</title>\n");
        sb.Append($"<link rel=\"stylesheet\" href=\"{Enc(cssHref)}\" />\n");
        sb.Append("<style>body{margin:0;}.topbar{display:none;}</style>\n");
        sb.Append("</head>\n<body>\n");
        sb.Append("<div id=\"swagger-ui\"></div>\n");
        sb.Append($"<script src=\"{Enc(bundleSrc)}\"></script>\n");
        sb.Append($"<script>window.addEventListener('load',function(){{{init}}});</script>\n");
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    /// <summary>Renders a deep-dive page (authentication / errors / rate-limits) in the shell.</summary>
    public static string RenderDeepDivePage(ApiDocsOptions options, string slug)
    {
        var (heading, eyebrow, body) = DeepDiveContent(slug, options);
        var refRoute = NormalizeRoute(options.ReferenceRoute);
        var cssHref = refRoute + "/assets/" + EmbeddedAsset.DocsCss;
        var jsSrc = refRoute + "/assets/" + EmbeddedAsset.DocsJs;

        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\" />\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />\n");
        sb.Append($"<title>{Enc(heading)} — {Enc(options.Title)}</title>\n");
        sb.Append($"<link rel=\"stylesheet\" href=\"{Enc(cssHref)}\" />\n");
        sb.Append(BrandStyle(options));
        sb.Append("</head>\n<body>\n");
        sb.Append("<a class=\"skip-link\" href=\"#content\">Skip to content</a>\n");
        sb.Append("<div class=\"app\">\n");
        sb.Append(Sidebar(options, slug));
        sb.Append("<div class=\"main\">\n");
        sb.Append("<div class=\"topbar\"><button class=\"menu-btn\" aria-expanded=\"false\">Menu</button></div>\n");
        sb.Append("<main class=\"content\" id=\"content\">\n");
        sb.Append($"<span class=\"eyebrow\">{Enc(eyebrow)}</span>\n");
        sb.Append($"<h1>{Enc(heading)}</h1>\n");
        sb.Append(body);
        sb.Append(Footer(options));
        sb.Append("</main>\n</div>\n");
        sb.Append("<div class=\"scrim\"></div>\n");
        sb.Append("</div>\n");
        sb.Append($"<script src=\"{Enc(jsSrc)}\"></script>\n");
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    /// <summary>True when the slug is one of the built-in deep-dive pages.</summary>
    public static bool IsKnownDeepDiveSlug(string slug) =>
        slug is AuthenticationSlug or ErrorsSlug or RateLimitsSlug;

    /// <summary>Normalizes a route to a leading-slash, no-trailing-slash form.</summary>
    public static string NormalizeRoute(string route)
    {
        var trimmed = (route ?? string.Empty).Trim();
        if (trimmed.Length == 0) trimmed = "/";
        if (!trimmed.StartsWith('/')) trimmed = "/" + trimmed;
        if (trimmed.Length > 1 && trimmed.EndsWith('/')) trimmed = trimmed.TrimEnd('/');
        return trimmed;
    }

    // ---- fragments ----

    private static string ReferenceHeader(ApiDocsOptions options, string refRoute)
    {
        var sb = new StringBuilder();
        sb.Append("<header class=\"ref-head\"><div class=\"ref-head-inner\">\n");
        sb.Append("<div>");
        sb.Append(BrandMarkInline(options));
        sb.Append($"<span class=\"eyebrow-inline\">{Enc(options.Eyebrow)}</span>");
        sb.Append($"<strong>{Enc(options.EffectiveServiceName)}</strong></div>\n");
        sb.Append($"<span class=\"sub\">{Enc(options.Description)}</span>\n");
        sb.Append("<span class=\"spacer\"></span>\n");
        if (options.IncludeDeepDivePages)
            sb.Append($"<a class=\"ref-link\" href=\"{Enc(refRoute + "/" + AuthenticationSlug)}\">Authentication</a>\n");
        if (options.EnableSwaggerUi)
            sb.Append($"<a class=\"ref-link\" href=\"{Enc(NormalizeRoute(options.SwaggerUiRoute))}\">Swagger UI</a>\n");
        sb.Append($"<a class=\"ref-link\" href=\"{Enc(options.SpecUrl)}\">Raw spec</a>\n");
        foreach (var link in options.NavLinks)
            sb.Append(NavAnchor(link, "ref-link"));
        sb.Append("</div></header>\n");
        return sb.ToString();
    }

    private static string ReferenceInitScript(string config, string specUrl, ApiDocsOptions options)
    {
        var sb = new StringBuilder();
        sb.Append("<script>(function(){\n");
        sb.Append("var loading=document.getElementById('ref-loading');\n");
        sb.Append($"var spec={JsStr(specUrl)};\n");
        sb.Append("function fail(m){if(loading){loading.innerHTML=m+' The raw spec is at <a href=\"'+spec+'\">'+spec+'</a>.';}}\n");
        sb.Append("try{\n");
        sb.Append("if(window.Scalar&&typeof window.Scalar.createApiReference==='function'){\n");
        sb.Append("if(loading){loading.remove();}\n");
        sb.Append($"window.Scalar.createApiReference('#ref-app',{config});\n");
        sb.Append("}else{fail('The interactive reference failed to load.');}\n");
        sb.Append("}catch(e){fail('The interactive reference failed to load.');}\n");
        sb.Append("fetch(spec,{method:'GET'}).then(function(r){if(!r.ok){fail('The API specification is temporarily unavailable (HTTP '+r.status+').');}}).catch(function(){});\n");
        sb.Append("})();</script>\n");
        return sb.ToString();
    }

    private static string Sidebar(ApiDocsOptions options, string activeSlug)
    {
        var refRoute = NormalizeRoute(options.ReferenceRoute);
        var sb = new StringBuilder();
        sb.Append("<aside class=\"sidebar\">\n");
        sb.Append("<div class=\"brand\">");
        sb.Append(BrandMarkInline(options));
        sb.Append($"<div><div class=\"brand-name\">{Enc(options.EffectiveServiceName)}</div>");
        sb.Append($"<div class=\"brand-sub\">{Enc(options.Eyebrow)}</div></div></div>\n");

        sb.Append("<nav class=\"nav-group\"><h4>Reference</h4>\n");
        sb.Append($"<a href=\"{Enc(refRoute)}\">API reference</a>\n");
        if (options.EnableSwaggerUi)
            sb.Append($"<a href=\"{Enc(NormalizeRoute(options.SwaggerUiRoute))}\">Swagger UI</a>\n");
        sb.Append($"<a href=\"{Enc(options.SpecUrl)}\">Raw OpenAPI spec</a>\n");
        sb.Append("</nav>\n");

        if (options.IncludeDeepDivePages)
        {
            sb.Append("<nav class=\"nav-group\"><h4>Guides</h4>\n");
            sb.Append(DeepDiveAnchor(refRoute, AuthenticationSlug, "Authentication", activeSlug));
            sb.Append(DeepDiveAnchor(refRoute, ErrorsSlug, "Errors", activeSlug));
            sb.Append(DeepDiveAnchor(refRoute, RateLimitsSlug, "Rate limits", activeSlug));
            sb.Append("</nav>\n");
        }

        if (options.NavLinks.Count > 0)
        {
            sb.Append("<nav class=\"nav-group\"><h4>Links</h4>\n");
            foreach (var link in options.NavLinks)
                sb.Append(NavAnchor(link, null));
            sb.Append("</nav>\n");
        }

        sb.Append($"<div class=\"side-foot\">{Attribution(options)}</div>\n");
        sb.Append("</aside>\n");
        return sb.ToString();
    }

    private static string DeepDiveAnchor(string refRoute, string slug, string label, string activeSlug)
    {
        var cls = slug == activeSlug ? " class=\"active\"" : string.Empty;
        return $"<a{cls} href=\"{Enc(refRoute + "/" + slug)}\">{Enc(label)}</a>\n";
    }

    private static string NavAnchor(DocsNavLink link, string? cssClass)
    {
        var cls = cssClass is null ? string.Empty : $" class=\"{cssClass}\"";
        var target = link.NewTab ? " target=\"_blank\" rel=\"noopener noreferrer\"" : string.Empty;
        return $"<a{cls} href=\"{Enc(link.Href)}\"{target}>{Enc(link.Label)}</a>\n";
    }

    private static string BrandMarkInline(ApiDocsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.LogoSvg))
            return $"<span class=\"brand-mark\">{options.LogoSvg}</span>";
        var name = options.EffectiveServiceName;
        var letter = string.IsNullOrEmpty(name) ? "A" : name.Trim()[..1].ToUpperInvariant();
        return $"<span class=\"brand-mark\">{Enc(letter)}</span>";
    }

    private static string BrandStyle(ApiDocsOptions options)
    {
        var accent = CssColor(options.BrandColor, ApiDocsOptions.DefaultBrandColor);
        var accent2 = CssColor(options.BrandColorSecondary, ApiDocsOptions.DefaultBrandColorSecondary);
        return $"<style>:root{{--accent:{accent};--accent-deep:{accent};--accent-2:{accent2};}}</style>\n";
    }

    private static string Attribution(ApiDocsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Attribution)) return string.Empty;
        if (options.AttributionAsLink && !string.IsNullOrWhiteSpace(options.AttributionUrl))
            return $"built by <a href=\"{Enc(options.AttributionUrl)}\" target=\"_blank\" rel=\"noopener noreferrer\">{Enc(options.Attribution)}</a>";
        return $"built by {Enc(options.Attribution)}";
    }

    private static string Footer(ApiDocsOptions options) =>
        $"<div class=\"foot\"><span>{Attribution(options)}</span></div>\n";

    private static (string Heading, string Eyebrow, string Body) DeepDiveContent(string slug, ApiDocsOptions options)
    {
        return slug switch
        {
            AuthenticationSlug => ("Authentication", "Guide", AuthenticationBody(options)),
            ErrorsSlug => ("Errors", "Guide", ErrorsBody()),
            RateLimitsSlug => ("Rate limits", "Guide", RateLimitsBody()),
            _ => ("Not found", "Guide", "<p>Unknown page.</p>"),
        };
    }

    private static string AuthenticationBody(ApiDocsOptions options)
    {
        return
            "<p class=\"lead\">Every non-public endpoint requires an authenticated caller. Send a bearer " +
            "token issued by the identity provider (Keycloak / OIDC) on each request.</p>\n" +
            "<h2>Bearer token</h2>\n" +
            "<p>Attach the access token in the <code>Authorization</code> header:</p>\n" +
            "<pre><code>Authorization: Bearer &lt;access_token&gt;</code></pre>\n" +
            "<div class=\"note\"><strong>Tip.</strong> Obtain a token via the OAuth2 authorization-code flow " +
            "(interactive apps) or client-credentials / direct-grant (service-to-service). The interactive " +
            "reference has an <em>Authorize</em> control that stores your token for try-it-out calls.</div>\n" +
            $"<h2>Where the spec lives</h2>\n<p>The machine-readable contract is served at " +
            $"<code>{Enc(options.SpecUrl)}</code>.</p>\n";
    }

    private static string ErrorsBody()
    {
        return
            "<p class=\"lead\">Errors are returned as RFC 7807 <code>application/problem+json</code> " +
            "payloads with a stable shape.</p>\n" +
            "<pre><code>{\n  \"title\": \"Resource was not found\",\n  \"status\": 404,\n  \"type\": \"NotFoundException\",\n  \"detail\": \"...\"\n}</code></pre>\n" +
            "<h2>Status codes</h2>\n<div class=\"table-wrap\"><table>\n" +
            "<thead><tr><th>Status</th><th>Meaning</th></tr></thead><tbody>\n" +
            "<tr><td><code>400</code></td><td>Validation failed — see <code>detail</code>.</td></tr>\n" +
            "<tr><td><code>401</code></td><td>Missing or invalid bearer token.</td></tr>\n" +
            "<tr><td><code>403</code></td><td>Authenticated but not authorized for the resource.</td></tr>\n" +
            "<tr><td><code>404</code></td><td>Resource does not exist (or is not visible to your tenant).</td></tr>\n" +
            "<tr><td><code>429</code></td><td>Rate limit exceeded — see the Rate limits guide.</td></tr>\n" +
            "<tr><td><code>500</code></td><td>Unexpected server error.</td></tr>\n" +
            "</tbody></table></div>\n";
    }

    private static string RateLimitsBody()
    {
        return
            "<p class=\"lead\">Requests are governed by named rate-limiting policies. When a limit is " +
            "exceeded the API responds <code>429 Too Many Requests</code>.</p>\n" +
            "<h2>Handling 429</h2>\n" +
            "<p>Honour the <code>Retry-After</code> response header (seconds) and back off before retrying.</p>\n" +
            "<pre><code>HTTP/1.1 429 Too Many Requests\nRetry-After: 30</code></pre>\n" +
            "<div class=\"note warn\"><strong>Be a good client.</strong> Add jittered exponential backoff; do " +
            "not hammer the endpoint the moment <code>Retry-After</code> elapses.</div>\n";
    }

    private static string CssColor(string? value, string fallback)
    {
        var v = (value ?? string.Empty).Trim();
        var ok = v.Length is > 0 and <= 32 && !v.Contains('<') && !v.Contains('>') && !v.Contains(';') && !v.Contains('{') && !v.Contains('}');
        return ok ? v : fallback;
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private static string JsStr(string value) => JsonSerializer.Serialize(value ?? string.Empty);
}
