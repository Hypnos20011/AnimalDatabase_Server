using System.Reflection;
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

    private static AuthorizationFilterContext Run(string? configuredKey, string? headerKey)
    {
        var settings = new Dictionary<string, string?>();
        if (configuredKey is not null)
        {
            settings[AdminKey.ConfigurationKey] = configuredKey;
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
        new RequireAdminKeyAttribute().OnAuthorization(context);
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
        Assert.Null(Run(Key, Key).Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Writes_fail_closed_with_503_when_no_key_is_configured(string? configured)
    {
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, StatusOf(Run(configured, Key)));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, StatusOf(Run(configured, null)));
    }

    [Fact]
    public void Matches_compares_whole_keys()
    {
        Assert.True(AdminKey.Matches(Key, Key));
        Assert.False(AdminKey.Matches(Key, Key[..^1]));
        Assert.False(AdminKey.Matches(Key, null));
    }

    [Fact]
    public void Admin_check_returns_204()
    {
        Assert.IsType<NoContentResult>(new AdminController().Check());
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
        action.GetCustomAttribute<RequireAdminKeyAttribute>() is not null
        || controller.GetCustomAttribute<RequireAdminKeyAttribute>() is not null;

    [Fact]
    public void Every_animal_write_needs_the_key()
    {
        var writes = Endpoints()
            .Where(endpoint => endpoint.Controller == typeof(AnimalsController) && endpoint.Method != "GET")
            .ToList();

        Assert.Equal(new[] { "DELETE", "POST", "PUT", "PUT" }, writes.Select(endpoint => endpoint.Method).OrderBy(method => method));
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
