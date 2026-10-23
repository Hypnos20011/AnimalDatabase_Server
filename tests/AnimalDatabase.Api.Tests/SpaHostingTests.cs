using System.Net;
using System.Text.Json;
using AnimalDatabase.Api.Hosting;

namespace AnimalDatabase.Api.Tests;

public sealed class SpaHostingTests : IDisposable
{
    private const string IndexMarker = "<app-root>spa-index</app-root>";
    private const string HashedScript = "main-ABCD2345.js";

    private readonly string _root;

    public SpaHostingTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "adb-spa-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "index.html"), $"<!doctype html><html><body>{IndexMarker}</body></html>");
        File.WriteAllText(Path.Combine(_root, HashedScript), "console.log('app');");
        File.WriteAllText(Path.Combine(_root, "favicon.ico"), "icon");
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private ApiFactory Factory(string environment = "Production") =>
        new(environment, new() { ["Spa:RootPath"] = _root });

    [Theory]
    [InlineData("/")]
    [InlineData("/index.html")]
    [InlineData("/admin")]
    [InlineData("/animals/12")]
    [InlineData("/animals/12/edit")]
    public async Task Site_routes_return_index_html_without_caching(string path)
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(IndexMarker, await response.Content.ReadAsStringAsync());
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Hashed_assets_are_cached_for_a_year()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/" + HashedScript);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SpaHosting.HashedAssetCache, response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Unhashed_assets_get_a_short_cache()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/favicon.ico");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(SpaHosting.OtherAssetCache, response.Headers.CacheControl?.ToString());
    }

    [Fact]
    public async Task Missing_files_are_not_answered_with_index_html()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/main-ZZZZ9999.js");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/")]
    [InlineData("/api/nope")]
    [InlineData("/api/animals/abc/def")]
    [InlineData("/API/Unknown")]
    [InlineData("/api/index.html")]
    public async Task Unknown_api_paths_return_404_json(string path)
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(IndexMarker, body);
        using var document = JsonDocument.Parse(body);
        Assert.Equal(404, document.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Unknown_api_paths_return_404_json_for_other_methods()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/nope", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Unknown_api_paths_return_404_json_when_no_site_is_deployed()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Swagger_is_not_served_outside_development_and_never_falls_back_to_the_site(string path)
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain(IndexMarker, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Swagger_still_works_in_development_when_the_site_is_served()
    {
        using var factory = Factory("Development");
        var client = factory.CreateClient();

        using var json = await client.GetAsync("/swagger/v1/swagger.json");
        var html = await client.GetStringAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, json.StatusCode);
        Assert.DoesNotContain(IndexMarker, html);
        Assert.Contains("swagger-ui", html);
    }

    [Fact]
    public async Task Api_routes_still_reach_controllers()
    {
        using var factory = Factory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/admin/check");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Site_is_not_served_when_disabled()
    {
        using var factory = new ApiFactory("Production", new() { ["Spa:RootPath"] = _root, ["Spa:Enabled"] = "false" });
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/animals/12");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("index.html", "no-cache")]
    [InlineData("main-ABCD2345.js", SpaHosting.HashedAssetCache)]
    [InlineData("styles-5INURTSO.css", SpaHosting.HashedAssetCache)]
    [InlineData("chunk-X7Q2LMNO.js", SpaHosting.HashedAssetCache)]
    [InlineData("favicon.ico", SpaHosting.OtherAssetCache)]
    [InlineData("3rdpartylicenses.txt", SpaHosting.OtherAssetCache)]
    public void Cache_control_depends_on_the_file_name(string name, string expected)
    {
        Assert.Equal(expected, SpaHosting.CacheControlFor(name));
    }
}
