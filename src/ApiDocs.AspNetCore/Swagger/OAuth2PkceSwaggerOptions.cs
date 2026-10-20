using Microsoft.Extensions.Configuration;

namespace ApiDocs.AspNetCore.Swagger;

/// <summary>Settings for the OAuth2 Authorization Code + PKCE login shown in Swagger UI.</summary>
public sealed class OAuth2PkceSwaggerOptions
{
    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public IDictionary<string, string> Scopes { get; set; } = new Dictionary<string, string>();

    public string Description { get; set; } = "Keycloak OAuth2 login (Authorization Code with PKCE)";

    public string? Title { get; set; }

    public string? Version { get; set; }

    /// <summary>Reads the Swagger:OAuth section, falling back to Jwt:Authority for the authority.</summary>
    public static OAuth2PkceSwaggerOptions FromConfiguration(IConfiguration configuration)
    {
        var swagger = configuration.GetSection("Swagger");
        return new OAuth2PkceSwaggerOptions
        {
            Authority = swagger["OAuth:Authority"] ?? configuration["Jwt:Authority"],
            ClientId = swagger["OAuth:ClientId"],
            Scopes = swagger.GetSection("OAuth:Scopes")
                .GetChildren()
                .ToDictionary(scope => scope.Key, scope => scope.Value ?? string.Empty),
        };
    }
}
