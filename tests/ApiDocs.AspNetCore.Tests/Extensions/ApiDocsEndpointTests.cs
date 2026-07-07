using System.Net;
using ApiDocs.AspNetCore.Configuration;
using ApiDocs.AspNetCore.Extensions;
using ApiDocs.AspNetCore.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace ApiDocs.AspNetCore.Tests.Extensions;

/// <summary>
/// Integration tests over a real (in-memory TestServer) host proving the one-line
/// <c>MapApiDocs</c> wiring serves the branded reference, raw Swagger UI, and vendored
/// assets, and that auth gating is applied.
/// </summary>
public class ApiDocsEndpointTests
{
    private static WebApplication BuildApp(ApiDocsOptions options, string environment = "Development")
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = environment,
        });
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.MapApiDocs(options);
        return app;
    }

    private static ApiDocsOptions OpenOptions() => new()
    {
        Title = "Kefi API",
        ServiceName = "Kefi",
        AuthMode = ApiDocsAuthMode.AlwaysOpen,
    };

    [Fact]
    public async Task MapApiDocs_GetReferenceRoute_Returns200HtmlReferencingVendoredScalar()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/docs");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
        body.ShouldContain("/docs/assets/scalar.standalone.js");
        body.ShouldNotContain("cdn.");
        body.ShouldNotContain("://");
    }

    [Fact]
    public async Task MapApiDocs_GetVendoredScalarAsset_Returns200JavaScriptBundle()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/docs/assets/scalar.standalone.js");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/javascript");
        body.Length.ShouldBeGreaterThan(1_000_000);
        body.ShouldContain("@scalar/api-reference");
    }

    [Fact]
    public async Task MapApiDocs_GetSwaggerUiRoute_Returns200AndIsReachable()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var page = await client.GetAsync("/swagger-ui");
        var bundle = await client.GetAsync("/swagger-ui/assets/swagger-ui-bundle.js");
        var css = await client.GetAsync("/swagger-ui/assets/swagger-ui.css");
        var pageBody = await page.Content.ReadAsStringAsync();

        // Assert
        page.StatusCode.ShouldBe(HttpStatusCode.OK);
        pageBody.ShouldContain("swagger-ui");
        bundle.StatusCode.ShouldBe(HttpStatusCode.OK);
        css.StatusCode.ShouldBe(HttpStatusCode.OK);
        css.Content.Headers.ContentType!.MediaType.ShouldBe("text/css");
    }

    [Fact]
    public async Task MapApiDocs_GetKnownDeepDivePage_Returns200()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/docs/authentication");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldContain("Authentication");
    }

    [Fact]
    public async Task MapApiDocs_GetUnknownDeepDivePage_Returns404()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/docs/not-a-real-guide");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MapApiDocs_GetUnknownAsset_Returns404()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/docs/assets/../secret.js");
        var direct = await client.GetAsync("/docs/assets/unknown.js");

        // Assert
        direct.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.StatusCode.ShouldNotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapApiDocs_WhenSwaggerUiDisabled_DoesNotServeSwaggerRoute()
    {
        // Arrange
        var options = OpenOptions();
        options.EnableSwaggerUi = false;
        await using var app = BuildApp(options);
        await app.StartAsync();
        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/swagger-ui");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MapApiDocs_WhenAlwaysRequireAuth_ReferenceEndpointCarriesAuthorizeMetadata()
    {
        // Arrange
        var options = OpenOptions();
        options.AuthMode = ApiDocsAuthMode.AlwaysRequireAuth;
        await using var app = BuildApp(options);
        await app.StartAsync();

        // Act
        var endpoint = FindEndpoint(app, "/docs");

        // Assert
        endpoint.ShouldNotBeNull();
        endpoint!.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
    }

    [Fact]
    public async Task MapApiDocs_WhenAlwaysOpen_ReferenceEndpointCarriesAllowAnonymousMetadata()
    {
        // Arrange
        await using var app = BuildApp(OpenOptions());
        await app.StartAsync();

        // Act
        var endpoint = FindEndpoint(app, "/docs");

        // Assert
        endpoint.ShouldNotBeNull();
        endpoint!.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
    }

    [Fact]
    public async Task MapApiDocs_WhenRequireAuthInProductionAndEnvIsProduction_RequiresAuth()
    {
        // Arrange — default AuthMode, Production environment.
        var options = OpenOptions();
        options.AuthMode = ApiDocsAuthMode.RequireAuthInProduction;
        await using var app = BuildApp(options, environment: "Production");
        await app.StartAsync();

        // Act
        var endpoint = FindEndpoint(app, "/docs");

        // Assert
        endpoint!.Metadata.GetMetadata<IAuthorizeData>().ShouldNotBeNull();
    }

    [Fact]
    public async Task MapApiDocs_WhenRequireAuthInProductionAndEnvIsDevelopment_IsOpen()
    {
        // Arrange
        var options = OpenOptions();
        options.AuthMode = ApiDocsAuthMode.RequireAuthInProduction;
        await using var app = BuildApp(options, environment: "Development");
        await app.StartAsync();

        // Act
        var endpoint = FindEndpoint(app, "/docs");

        // Assert
        endpoint!.Metadata.GetMetadata<IAllowAnonymous>().ShouldNotBeNull();
    }

    private static Endpoint? FindEndpoint(WebApplication app, string rawText)
    {
        var source = app.Services.GetRequiredService<EndpointDataSource>();
        return source.Endpoints
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => string.Equals(
                DocsPageRenderer.NormalizeRoute(e.RoutePattern.RawText ?? string.Empty),
                rawText,
                StringComparison.OrdinalIgnoreCase));
    }
}
