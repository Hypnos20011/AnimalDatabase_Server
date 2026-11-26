using System.Net;
using System.Reflection;
using System.Text.Json;
using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnimalDatabase.Api.Tests;

public class AdminKeyTests
{
    private const string Key = "test-admin-key-0123456789";
    private const string TesterKey = "test-tester-key-0123456789";

    private static AuthorizationFilterContext Run(
        string? configuredKey,
        string? headerKey,
        string? configuredTesterKey = null,
        Role requiredRole = Role.Admin)
    {
        var settings = new Dictionary<string, string?>();
        if (configuredKey is not null)
        {
            settings[AdminKey.ConfigurationKey] = configuredKey;
        }
        if (configuredTesterKey is not null)
        {
            settings[AdminKey.TesterConfigurationKey] = configuredTesterKey;
        }
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = services };
        if (headerKey is not null)
        {
            httpContext.Request.Headers[AdminKey.HeaderName] = headerKey;
        }
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        new RequireRoleAttribute(requiredRole).OnAuthorization(context);
        return context;
    }

    private static int? StatusOf(AuthorizationFilterContext context) =>
        (context.Result as ObjectResult)?.StatusCode;

    [Fact]
    public void Missing_key_is_rejected_with_401()
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(Run(Key, null)));
    }

    [Fact]
    public void Empty_key_is_rejected_with_401()
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(Run(Key, "")));
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("test-admin-key-0123456788")]
    [InlineData("test-admin-key-0123456789 ")]
    [InlineData("TEST-ADMIN-KEY-0123456789")]
    public void Wrong_key_is_rejected_with_401(string provided)
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(Run(Key, provided)));
    }

    [Fact]
    public void Right_key_passes()
    {
        var context = Run(Key, Key);
        Assert.Null(context.Result);
        Assert.Equal(Role.Admin, context.HttpContext.Items[RequireRoleAttribute.RoleItemKey]);
    }

    [Fact]
    public void Tester_key_passes_tester_role_and_is_stored_in_http_context()
    {
        var context = Run(Key, TesterKey, TesterKey, Role.Tester);

        Assert.Null(context.Result);
        Assert.Equal(Role.Tester, context.HttpContext.Items[RequireRoleAttribute.RoleItemKey]);
    }

    [Fact]
    public void Tester_key_is_forbidden_for_admin_role()
    {
        var context = Run(Key, TesterKey, TesterKey);

        Assert.Equal(StatusCodes.Status403Forbidden, StatusOf(context));
        Assert.Equal(Role.Tester, context.HttpContext.Items[RequireRoleAttribute.RoleItemKey]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_tester_key_is_rejected_with_401(string? configuredTesterKey)
    {
        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(Run(Key, TesterKey, configuredTesterKey, Role.Tester)));
    }

    [Fact]
    public void Admin_key_takes_precedence_when_keys_match()
    {
        var context = Run(Key, Key, Key, Role.Tester);

        Assert.Null(context.Result);
        Assert.Equal(Role.Admin, context.HttpContext.Items[RequireRoleAttribute.RoleItemKey]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Writes_fail_closed_with_503_when_no_key_is_configured(string? configured)
    {
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, StatusOf(Run(configured, Key)));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, StatusOf(Run(configured, null)));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable,
            StatusOf(Run(configured, TesterKey, TesterKey, Role.Tester)));
    }

    [Fact]
    public void Matches_compares_whole_keys()
    {
        Assert.True(AdminKey.Matches(Key, Key));
        Assert.False(AdminKey.Matches(Key, Key[..^1]));
        Assert.False(AdminKey.Matches(Key, null));
    }

    [Fact]
    public async Task Admin_check_returns_role_for_each_key()
    {
        await AssertCheckRole(ApiFactory.TestKey, "admin");
        await AssertCheckRole(ApiFactory.TesterKey, "tester");
    }

    private static async Task AssertCheckRole(string key, string expectedRole)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
        request.Headers.Add(AdminKey.HeaderName, key);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedRole, document.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Tester_key_is_allowed_for_wikipedia_import()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/wikipedia/import?q=");
        request.Headers.Add(AdminKey.HeaderName, ApiFactory.TesterKey);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/missing-translations")]
    [InlineData("POST", "/api/animals")]
    [InlineData("PUT", "/api/animals/1")]
    [InlineData("PUT", "/api/animals/1/translations/de")]
    [InlineData("POST", "/api/animals/1/translate-missing")]
    [InlineData("DELETE", "/api/animals/1")]
    [InlineData("POST", "/api/translate?lang=de")]
    public async Task Tester_key_is_forbidden_for_each_admin_only_endpoint(string method, string path)
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add(AdminKey.HeaderName, ApiFactory.TesterKey);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Blank_tester_key_is_rejected_by_check_with_401()
    {
        using var factory = new ApiFactory(settings: new() { [AdminKey.TesterConfigurationKey] = " " });
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/check");
        request.Headers.Add(AdminKey.HeaderName, ApiFactory.TesterKey);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static IEnumerable<(Type Controller, MethodInfo Action, string Method)> Endpoints()
    {
        var controllers = typeof(AnimalsController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);
        foreach (var controller in controllers)
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var verb in action.GetCustomAttributes<HttpMethodAttribute>())
                {
                    foreach (var method in verb.HttpMethods)
                    {
                        yield return (controller, action, method);
                    }
                }
            }
        }
    }

    private static bool IsProtected(Type controller, MethodInfo action) =>
        action.GetCustomAttribute<RequireRoleAttribute>() is not null
        || controller.GetCustomAttribute<RequireRoleAttribute>() is not null;

    [Fact]
    public void Every_animal_write_needs_the_key()
    {
        var writes = Endpoints()
            .Where(endpoint => endpoint.Controller == typeof(AnimalsController) && endpoint.Method != "GET")
            .ToList();

        Assert.Equal(new[] { "DELETE", "POST", "POST", "PUT", "PUT" }, writes.Select(endpoint => endpoint.Method).OrderBy(method => method));
        Assert.All(writes, endpoint => Assert.True(IsProtected(endpoint.Controller, endpoint.Action), endpoint.Action.Name));
    }

    [Fact]
    public void Wikipedia_import_needs_the_key()
    {
        var import = typeof(WikipediaController).GetMethod(nameof(WikipediaController.Import))!;
        Assert.True(IsProtected(typeof(WikipediaController), import));
    }

    [Fact]
    public void Gets_and_search_logging_stay_open()
    {
        var open = Endpoints()
            .Where(endpoint => endpoint.Controller != typeof(AdminController)
                && endpoint.Controller != typeof(WikipediaController)
                && (endpoint.Method == "GET" || endpoint.Controller == typeof(SearchesController)))
            .ToList();

        Assert.Contains(open, endpoint => endpoint.Controller == typeof(SearchesController) && endpoint.Method == "POST");
        Assert.Contains(open, endpoint => endpoint.Controller == typeof(AnimalsController) && endpoint.Method == "GET");
        Assert.All(open, endpoint => Assert.False(IsProtected(endpoint.Controller, endpoint.Action), endpoint.Action.Name));
    }

}
