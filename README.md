# ApiDocs.AspNetCore

One-line, **branded**, **fully self-contained** OpenAPI docs portal for ASP.NET Core
services. Serves a branded **Scalar** interactive reference **and** the raw **Swagger UI**
over your existing `/swagger/v1/swagger.json` — with both browser bundles **vendored as
embedded resources** (no CDN, CSP-safe).

This is the reusable version of the hand-built AMLService docs portal: same polished shell,
but themeable per service and with the Scalar/Swagger assets shipped inside the package
instead of loaded from `cdn.jsdelivr.net`.

## Why

- **Consistent** — every service exposes the same polished reference with one line.
- **Self-contained / CSP-safe** — the served HTML references no external host. Scalar is
  configured with `withDefaultFonts:false` + empty `proxyUrl`; Swagger UI with
  `validatorUrl:null`; all assets are served same-origin from the package.
- **Branded** — title, colours, logo, eyebrow, deep-dive guide pages, extra nav links.
- **Swagger too** — the classic Swagger UI is served alongside the branded reference.

## Install

```xml
<PackageReference Include="ApiDocs.AspNetCore" Version="1.0.0" />
```

## Quick start (adoption snippet)

The service must already produce an OpenAPI document at `/swagger/v1/swagger.json`
(FastEndpoints.Swagger / NSwag / Swashbuckle — anything that serves the JSON spec).

```csharp
using ApiDocs.AspNetCore.Configuration;
using ApiDocs.AspNetCore.Extensions;

var app = builder.Build();

// ... your middleware / endpoints ...

app.MapApiDocs(new ApiDocsOptions
{
    Title = "Kefi API",
    ServiceName = "Kefi",
    Eyebrow = "Developer docs",
    Description = "The Kefi events API — surveys, tickets, payments.",
    BrandColor = "#7c3aed",            // --accent
    BrandColorSecondary = "#14b8a6",   // --accent-2
    SpecUrl = "/swagger/v1/swagger.json",
    // AuthMode = ApiDocsAuthMode.RequireAuthInProduction  // default
});

app.Run();
```

That serves:

| Route | What |
|-------|------|
| `GET /docs` | Branded **Scalar** interactive reference (vendored) |
| `GET /docs/authentication`, `/docs/errors`, `/docs/rate-limits` | Deep-dive guide pages |
| `GET /docs/assets/*` | Vendored Scalar bundle + docs chrome (css/js) |
| `GET /swagger-ui` | Raw **Swagger UI** (vendored) |
| `GET /swagger-ui/assets/*` | Vendored Swagger UI bundle + css |

Both renderers read the same `SpecUrl`.

### Bind from configuration instead

```csharp
builder.AddApiDocs(opts => opts.Title = "Kefi API"); // binds the "ApiDocs" section + overrides
...
app.MapApiDocs(); // resolves the bound options from DI
```

```jsonc
// appsettings.json
"ApiDocs": {
  "Title": "Kefi API",
  "ServiceName": "Kefi",
  "BrandColor": "#7c3aed",
  "SpecUrl": "/swagger/v1/swagger.json"
}
```

## Options (`ApiDocsOptions`)

| Field | Default | Purpose |
|-------|---------|---------|
| `Title` | `"API"` | Page title + default brand name |
| `ServiceName` | = `Title` | Sidebar brand name |
| `Eyebrow` | `"Developer docs"` | Small uppercase label |
| `Description` | generic | Reference-header subtitle / meta description |
| `BrandColor` | `#4f46e5` | Accent (`--accent`), injected as inline CSS var |
| `BrandColorSecondary` | `#0fb5a6` | Secondary accent (`--accent-2`) |
| `LogoSvg` | `null` | Inline SVG brand mark (else a lettered mark) |
| `SpecUrl` | `/swagger/v1/swagger.json` | OpenAPI spec both renderers read |
| `ReferenceRoute` | `/docs` | Branded Scalar reference route prefix |
| `SwaggerUiRoute` | `/swagger-ui` | Raw Swagger UI route prefix |
| `EnableReference` | `true` | Map the Scalar reference |
| `EnableSwaggerUi` | `true` | Map the raw Swagger UI |
| `IncludeDeepDivePages` | `true` | Authentication / Errors / Rate limits pages |
| `ScalarTheme` | `"default"` | Scalar theme name |
| `ScalarDarkMode` | `false` | Scalar dark mode |
| `Attribution` / `AttributionUrl` | `dloizides.com` | Discreet "built by" |
| `AttributionAsLink` | `false` | Render attribution as a link (adds an external anchor); text otherwise (CSP-clean) |
| `AuthMode` | `RequireAuthInProduction` | `AlwaysOpen` \| `RequireAuthInProduction` \| `AlwaysRequireAuth` |
| `AuthorizationPolicy` | `null` | Named policy applied when auth is required |
| `NavLinks` | empty | Extra sidebar/header links |

## Auth gating

`AuthMode` defaults to **`RequireAuthInProduction`**, mirroring the platform posture
(e.g. kefi's `/swagger` returns 401 in prod, open in dev/staging):

- `AlwaysOpen` — open in every environment (public API products).
- `RequireAuthInProduction` — open in Development/Staging, requires authorization in
  Production (`IHostEnvironment.IsProduction()`).
- `AlwaysRequireAuth` — requires authorization everywhere.

When auth is required the docs group calls `RequireAuthorization()` (or your
`AuthorizationPolicy`); otherwise it calls `AllowAnonymous()` so a global auth fallback
doesn't accidentally gate the docs.

## Route conflicts

`SwaggerUiRoute` defaults to `/swagger-ui` (not `/swagger`) so it does **not** collide with
services that already serve NSwag's UI at `/swagger`. Set it to `/swagger` only for a
service that doesn't already own that route.

## Vendored assets

| Asset | Version | Served at |
|-------|---------|-----------|
| `@scalar/api-reference` | 1.62.4 | `{ReferenceRoute}/assets/scalar.standalone.js` |
| `swagger-ui-dist` | 5.x | `{SwaggerUiRoute}/assets/swagger-ui-bundle.js` + `.css` |
| docs chrome (adapted from AMLService) | — | `{ReferenceRoute}/assets/docs.css` + `docs.js` |

All are embedded in the assembly — the consuming service copies nothing to `wwwroot`.

## License

MIT
