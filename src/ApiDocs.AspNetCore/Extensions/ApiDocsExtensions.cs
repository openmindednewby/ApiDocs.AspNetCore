using ApiDocs.AspNetCore.Configuration;
using ApiDocs.AspNetCore.Rendering;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ApiDocs.AspNetCore.Extensions;

/// <summary>
/// One-line wiring for the shared, branded, self-contained API docs portal.
/// </summary>
public static class ApiDocsExtensions
{
    private const string CacheControlValue = "public, max-age=3600";

    private static readonly HashSet<string> ReferenceAssets = new(StringComparer.Ordinal)
    {
        EmbeddedAsset.ScalarBundle,
        EmbeddedAsset.DocsCss,
        EmbeddedAsset.DocsJs,
    };

    private static readonly HashSet<string> SwaggerAssets = new(StringComparer.Ordinal)
    {
        EmbeddedAsset.SwaggerUiBundle,
        EmbeddedAsset.SwaggerUiCss,
    };

    /// <summary>
    /// Optional: binds <see cref="ApiDocsOptions"/> from the "ApiDocs" configuration
    /// section (plus code overrides) into DI, so <see cref="MapApiDocs(IEndpointRouteBuilder, Action{ApiDocsOptions}?)"/>
    /// can resolve them. Not required when passing options explicitly to MapApiDocs.
    /// </summary>
    public static TBuilder AddApiDocs<TBuilder>(
        this TBuilder builder,
        Action<ApiDocsOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services
            .AddOptions<ApiDocsOptions>()
            .Bind(builder.Configuration.GetSection(ApiDocsOptions.SectionName))
            .Configure(opts => configure?.Invoke(opts));

        return builder;
    }

    /// <summary>
    /// Maps the branded Scalar reference + raw Swagger UI over the service's existing
    /// OpenAPI spec, resolving options from DI (see <see cref="AddApiDocs"/>) with an
    /// optional last-mile override.
    /// </summary>
    public static IEndpointRouteBuilder MapApiDocs(
        this IEndpointRouteBuilder endpoints,
        Action<ApiDocsOptions>? configure = null)
    {
        var resolved = endpoints.ServiceProvider.GetService<IOptions<ApiDocsOptions>>()?.Value;
        var options = resolved ?? new ApiDocsOptions();
        configure?.Invoke(options);
        return endpoints.MapApiDocs(options);
    }

    /// <summary>
    /// Maps the branded Scalar reference + raw Swagger UI using the supplied options.
    /// </summary>
    public static IEndpointRouteBuilder MapApiDocs(
        this IEndpointRouteBuilder endpoints,
        ApiDocsOptions options)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(options);

        var requireAuth = ShouldRequireAuth(options, endpoints.ServiceProvider);

        if (options.EnableReference)
            MapReference(endpoints, options, requireAuth);

        if (options.EnableSwaggerUi)
            MapSwaggerUi(endpoints, options, requireAuth);

        return endpoints;
    }

    private static void MapReference(IEndpointRouteBuilder endpoints, ApiDocsOptions options, bool requireAuth)
    {
        var route = DocsPageRenderer.NormalizeRoute(options.ReferenceRoute);
        var group = endpoints.MapGroup(route);
        ApplyAuth(group, options, requireAuth);

        group.MapGet("", () => Results.Content(DocsPageRenderer.RenderReferencePage(options), "text/html; charset=utf-8"));

        if (options.IncludeDeepDivePages)
        {
            group.MapGet("/{slug}", (string slug) =>
                DocsPageRenderer.IsKnownDeepDiveSlug(slug)
                    ? Results.Content(DocsPageRenderer.RenderDeepDivePage(options, slug), "text/html; charset=utf-8")
                    : Results.NotFound());
        }

        group.MapGet("/assets/{file}", (HttpContext ctx, string file) => ServeAssetAsync(ctx, file, ReferenceAssets));
    }

    private static void MapSwaggerUi(IEndpointRouteBuilder endpoints, ApiDocsOptions options, bool requireAuth)
    {
        var route = DocsPageRenderer.NormalizeRoute(options.SwaggerUiRoute);
        var group = endpoints.MapGroup(route);
        ApplyAuth(group, options, requireAuth);

        group.MapGet("", () => Results.Content(DocsPageRenderer.RenderSwaggerUiPage(options), "text/html; charset=utf-8"));
        group.MapGet("/assets/{file}", (HttpContext ctx, string file) => ServeAssetAsync(ctx, file, SwaggerAssets));
    }

    private static async Task ServeAssetAsync(HttpContext ctx, string file, HashSet<string> allowed)
    {
        if (!allowed.Contains(file))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var bytes = EmbeddedAsset.GetBytes(file);
        ctx.Response.ContentType = EmbeddedAsset.ContentTypeFor(file);
        ctx.Response.Headers.CacheControl = CacheControlValue;
        await ctx.Response.Body.WriteAsync(bytes);
    }

    private static void ApplyAuth(RouteGroupBuilder group, ApiDocsOptions options, bool requireAuth)
    {
        if (!requireAuth)
        {
            group.AllowAnonymous();
            return;
        }

        if (string.IsNullOrWhiteSpace(options.AuthorizationPolicy))
            group.RequireAuthorization();
        else
            group.RequireAuthorization(options.AuthorizationPolicy);
    }

    private static bool ShouldRequireAuth(ApiDocsOptions options, IServiceProvider services)
    {
        return options.AuthMode switch
        {
            ApiDocsAuthMode.AlwaysOpen => false,
            ApiDocsAuthMode.AlwaysRequireAuth => true,
            _ => IsProduction(services),
        };
    }

    private static bool IsProduction(IServiceProvider services)
    {
        var env = services.GetService<IHostEnvironment>();
        return env is not null && env.IsProduction();
    }
}
