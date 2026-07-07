namespace ApiDocs.AspNetCore.Configuration;

/// <summary>
/// Controls whether the docs endpoints (branded reference + raw Swagger UI + assets)
/// require an authenticated caller.
/// </summary>
public enum ApiDocsAuthMode
{
    /// <summary>
    /// Docs are open in every environment. Use for public API products whose reference
    /// is meant to be crawlable / shareable.
    /// </summary>
    AlwaysOpen = 0,

    /// <summary>
    /// Docs are open in Development/Staging but require authorization in Production.
    /// Mirrors the safe default used across the platform (e.g. kefi's 401-gated
    /// <c>/swagger</c> in prod). This is the default.
    /// </summary>
    RequireAuthInProduction = 1,

    /// <summary>
    /// Docs require authorization in every environment.
    /// </summary>
    AlwaysRequireAuth = 2,
}
