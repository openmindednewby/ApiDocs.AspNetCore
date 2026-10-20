using ApiDocs.AspNetCore.Swagger;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiDocs.AspNetCore.Extensions;

/// <summary>Registers the FastEndpoints Swagger document with the shared Keycloak OAuth2 + PKCE login.</summary>
public static class OAuth2PkceSwaggerExtensions
{
    /// <summary>Adds the Swagger document from configuration, with optional overrides.</summary>
    public static IServiceCollection AddOAuth2PkceSwaggerDocument(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<OAuth2PkceSwaggerOptions>? configure = null)
    {
        var options = OAuth2PkceSwaggerOptions.FromConfiguration(configuration);
        configure?.Invoke(options);

        return services.SwaggerDocument(document =>
        {
            document.ShortSchemaNames = true;
            document.EnableJWTBearerAuth = false;
            document.DocumentSettings = settings =>
            {
                if (options.Title is not null)
                    settings.Title = options.Title;
                if (options.Version is not null)
                    settings.Version = options.Version;
                settings.AddAuth(OAuth2PkceSecurity.SchemeName, OAuth2PkceSecurity.CreateScheme(options));
                settings.PostProcess = openApiDocument => OAuth2PkceSecurity.ApplyPkceClientHints(openApiDocument, options.ClientId);
            };
        });
    }
}
