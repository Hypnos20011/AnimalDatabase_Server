using System.Net;
using System.Text.Json;
using AnimalDatabase.Api.OpenApi;

namespace AnimalDatabase.Api.Tests;

public class SwaggerTests
{
    [Fact]
    public async Task Swagger_json_has_an_openapi_version_field()
    {
        using var factory = new ApiFactory("Development");
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var version = document.RootElement.GetProperty("openapi").GetString();
        Assert.Matches(@"^3\.\d+\.\d+$", version);
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("info").GetProperty("version").GetString()));
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/animals", out _));
    }

    [Fact]
    public async Task Swagger_ui_loads_its_scripts_with_a_version_so_old_cached_copies_are_not_used()
    {
        using var factory = new ApiFactory("Development");
        var client = factory.CreateClient();

        var html = await client.GetStringAsync("/swagger/index.html");
        var version = Uri.EscapeDataString(SwaggerUiAssets.Version);

        Assert.Contains($"src=\"./swagger-ui-bundle.js?v={version}\"", html);
        Assert.Contains($"src=\"./swagger-ui-standalone-preset.js?v={version}\"", html);
        Assert.Contains($"href=\"./swagger-ui.css?v={version}\"", html);
        Assert.Contains($"src=\"index.js?v={version}\"", html);
    }

    [Fact]
    public async Task Versioned_swagger_ui_scripts_are_still_served()
    {
        using var factory = new ApiFactory("Development");
        var client = factory.CreateClient();

        using var bundle = await client.GetAsync($"/swagger/swagger-ui-bundle.js?v={SwaggerUiAssets.Version}");
        using var index = await client.GetAsync($"/swagger/index.js?v={SwaggerUiAssets.Version}");

        Assert.Equal(HttpStatusCode.OK, bundle.StatusCode);
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
    }

    [Fact]
    public void Add_version_leaves_other_links_alone()
    {
        var html = "<a href=\"https://example.org/x.js\"></a><link href=\"./index.css\"><img src=\"./favicon-32x32.png\">";

        var result = SwaggerUiAssets.AddVersion(html, "1.2.3");

        Assert.Equal("<a href=\"https://example.org/x.js\"></a><link href=\"./index.css?v=1.2.3\"><img src=\"./favicon-32x32.png\">", result);
    }

    [Theory]
    [InlineData("./swagger-ui-bundle.js", "./swagger-ui-bundle.js?v=1.2.3")]
    [InlineData("./swagger-ui.css?v=9", "./swagger-ui.css?v=9")]
    public void With_version_adds_the_version_once(string path, string expected)
    {
        Assert.Equal(expected, SwaggerUiAssets.WithVersion(path, "1.2.3"));
    }
}
