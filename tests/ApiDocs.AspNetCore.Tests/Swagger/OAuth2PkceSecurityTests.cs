using ApiDocs.AspNetCore.Swagger;
using Microsoft.Extensions.Configuration;
using NSwag;
using Shouldly;

namespace ApiDocs.AspNetCore.Tests.Swagger;

public class OAuth2PkceSecurityTests
{
    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void FromConfiguration_WithSwaggerAuthority_UsesSwaggerSettings()
    {
        var configuration = Configuration(new()
        {
            ["Jwt:Authority"] = "http://internal/realms/kefi",
            ["Swagger:OAuth:Authority"] = "https://auth.example.com/realms/kefi",
            ["Swagger:OAuth:ClientId"] = "kefi-swagger",
            ["Swagger:OAuth:Scopes:openid"] = "OpenID",
        });

        var options = OAuth2PkceSwaggerOptions.FromConfiguration(configuration);

        options.Authority.ShouldBe("https://auth.example.com/realms/kefi");
        options.ClientId.ShouldBe("kefi-swagger");
        options.Scopes.ShouldBe(new Dictionary<string, string> { ["openid"] = "OpenID" });
    }

    [Fact]
    public void FromConfiguration_WithoutSwaggerAuthority_FallsBackToJwtAuthority()
    {
        var configuration = Configuration(new() { ["Jwt:Authority"] = "http://internal/realms/kefi" });

        var options = OAuth2PkceSwaggerOptions.FromConfiguration(configuration);

        options.Authority.ShouldBe("http://internal/realms/kefi");
        options.Scopes.ShouldBeEmpty();
    }

    [Fact]
    public void CreateScheme_WithAuthority_BuildsKeycloakAuthorizationCodeFlow()
    {
        var options = new OAuth2PkceSwaggerOptions
        {
            Authority = "https://auth.example.com/realms/kefi",
            Scopes = new Dictionary<string, string> { ["profile"] = "Profile" },
        };

        var scheme = OAuth2PkceSecurity.CreateScheme(options);

        scheme.Type.ShouldBe(OpenApiSecuritySchemeType.OAuth2);
        scheme.Flows.AuthorizationCode.AuthorizationUrl.ShouldBe("https://auth.example.com/realms/kefi/protocol/openid-connect/auth");
        scheme.Flows.AuthorizationCode.TokenUrl.ShouldBe("https://auth.example.com/realms/kefi/protocol/openid-connect/token");
        scheme.Flows.AuthorizationCode.Scopes.ShouldBe(new Dictionary<string, string> { ["profile"] = "Profile" });
    }

    [Fact]
    public void ApplyPkceClientHints_WithOAuth2Scheme_AddsClientIdAndPkceFlag()
    {
        var document = new OpenApiDocument();
        document.SecurityDefinitions[OAuth2PkceSecurity.SchemeName] = new OpenApiSecurityScheme();

        OAuth2PkceSecurity.ApplyPkceClientHints(document, "kefi-swagger");

        var extensions = document.SecurityDefinitions["oauth2"].ExtensionData!;
        extensions["x-clientId"].ShouldBe("kefi-swagger");
        extensions["x-usePkceWithAuthorizationCodeGrant"].ShouldBe(true);
    }

    [Fact]
    public void ApplyPkceClientHints_WithoutOAuth2Scheme_LeavesDocumentUnchanged()
    {
        var document = new OpenApiDocument();

        OAuth2PkceSecurity.ApplyPkceClientHints(document, "kefi-swagger");

        document.SecurityDefinitions.ShouldBeEmpty();
    }
}
