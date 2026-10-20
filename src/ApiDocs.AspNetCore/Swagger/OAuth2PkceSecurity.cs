using NSwag;

namespace ApiDocs.AspNetCore.Swagger;

/// <summary>Builds the NSwag OAuth2 + PKCE security scheme and the Swagger UI client hints.</summary>
public static class OAuth2PkceSecurity
{
    public const string SchemeName = "oauth2";

    public static OpenApiSecurityScheme CreateScheme(OAuth2PkceSwaggerOptions options) => new()
    {
        Type = OpenApiSecuritySchemeType.OAuth2,
        Description = options.Description,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = $"{options.Authority}/protocol/openid-connect/auth",
                TokenUrl = $"{options.Authority}/protocol/openid-connect/token",
                Scopes = new Dictionary<string, string>(options.Scopes),
            },
        },
    };

    public static void ApplyPkceClientHints(OpenApiDocument document, string? clientId)
    {
        if (!document.SecurityDefinitions.TryGetValue(SchemeName, out var scheme))
            return;

        scheme.ExtensionData = new Dictionary<string, object?>
        {
            ["x-clientId"] = clientId,
            ["x-usePkceWithAuthorizationCodeGrant"] = true,
        };
    }
}
