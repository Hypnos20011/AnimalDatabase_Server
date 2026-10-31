using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace AnimalDatabase.Api.Hosting;

public static partial class SpaHosting
{
    public const string IndexFile = "index.html";
    public const string HashedAssetCache = "public, max-age=31536000, immutable";
    public const string IndexCache = "no-cache";
    public const string OtherAssetCache = "public, max-age=3600";

    private static readonly string[] ReservedPrefixes = { "/api", "/swagger" };

    public static string? ResolveRoot(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var settings = configuration.GetSection(SpaSettings.SectionName).Get<SpaSettings>() ?? new SpaSettings();
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.RootPath)) return null;

        var root = Path.GetFullPath(settings.RootPath, environment.ContentRootPath);
        return File.Exists(Path.Combine(root, IndexFile)) ? root : null;
    }

    public static WebApplication UseSpaFiles(this WebApplication app, string root)
    {
        var files = new PhysicalFileProvider(root);

        app.UseWhen(context => !IsReserved(context.Request.Path), branch =>
        {
            branch.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            branch.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = new FileExtensionContentTypeProvider(),
                OnPrepareResponse = context => ApplyCacheHeaders(context.Context.Response, context.File.Name)
            });
        });

        return app;
    }

    public static WebApplication MapSpaFallback(this WebApplication app, string root)
    {
        var files = new PhysicalFileProvider(root);
        app.MapFallbackToFile(IndexFile, new StaticFileOptions
        {
            FileProvider = files,
            OnPrepareResponse = context => ApplyCacheHeaders(context.Context.Response, IndexFile)
        });
        return app;
    }

    public static WebApplication MapReservedNotFound(this WebApplication app)
    {
        foreach (var prefix in ReservedPrefixes) app.MapFallback(prefix + "/{**path}", NotFound);
        return app;
    }

    public static bool IsReserved(PathString path)
    {
        return ReservedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public static string CacheControlFor(string fileName)
    {
        if (string.Equals(fileName, IndexFile, StringComparison.OrdinalIgnoreCase)) return IndexCache;
        return HashedName().IsMatch(fileName) ? HashedAssetCache : OtherAssetCache;
    }

    private static void ApplyCacheHeaders(HttpResponse response, string fileName)
    {
        response.Headers[HeaderNames.CacheControl] = CacheControlFor(fileName);
    }

    private static IResult NotFound(HttpContext context)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"No endpoint matches {context.Request.Method} {context.Request.Path}.");
    }

    [GeneratedRegex(@"-[A-Za-z0-9]{8,}\.[A-Za-z0-9]+$")]
    private static partial Regex HashedName();
}