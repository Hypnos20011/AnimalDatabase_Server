using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AnimalDatabase.Api.Controllers;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AnimalDatabase.Api.Tests;

public class TranslateControllerTests
{
    private sealed class StubDeeplHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Exception? Failure { get; set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (Failure is not null)
            {
                throw Failure;
            }
            if (Status != HttpStatusCode.OK)
            {
                return new HttpResponseMessage(Status);
            }
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var translations = json.RootElement.GetProperty("text").EnumerateArray()
                .Select(t => new { text = "DE:" + t.GetString() }).ToArray();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { translations }) };
        }
    }

    private static (TranslateController Controller, StubDeeplHandler Handler) Create()
    {
        var handler = new StubDeeplHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-free.deepl.com/") };
        var translator = new AnimalTextTranslator(new DeeplTranslator(http));
        return (new TranslateController(translator), handler);
    }

    private static AnimalTexts Okapi() => new()
    {
        CommonName = "Okapi",
        Habitat = "Rainforest",
        RangeRegion = "Central Africa",
        Profile = "A forest giraffe. Fun fact: Its tongue is very long."
    };

    [Fact]
    public async Task Valid_texts_return_ok_with_translations()
    {
        var (controller, handler) = Create();

        var response = await controller.Translate(Okapi());

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        var result = Assert.IsType<AnimalTexts>(ok.Value);
        Assert.Equal("DE:Okapi", result.CommonName);
        Assert.Equal("DE:Rainforest", result.Habitat);
        Assert.Equal("DE:Central Africa", result.RangeRegion);
        Assert.Equal("DE:A forest giraffe. Fun fact: DE:Its tongue is very long.", result.Profile);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Exactly_3000_characters_are_allowed()
    {
        var (controller, handler) = Create();
        var texts = new AnimalTexts { CommonName = new string('a', 1000), Profile = new string('b', 2000) };

        var response = await controller.Translate(texts);

        Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task More_than_3000_characters_return_bad_request_without_calling_deepl()
    {
        var (controller, handler) = Create();
        var texts = new AnimalTexts
        {
            CommonName = new string('a', 1000),
            Habitat = new string('b', 1000),
            RangeRegion = new string('c', 500),
            Profile = new string('d', 501)
        };

        var response = await controller.Translate(texts);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Used_up_credit_returns_service_unavailable()
    {
        var (controller, handler) = Create();
        handler.Status = (HttpStatusCode)456;

        var response = await controller.Translate(Okapi());

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(503, result.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Other_deepl_errors_return_bad_gateway(HttpStatusCode status)
    {
        var (controller, handler) = Create();
        handler.Status = status;

        var response = await controller.Translate(Okapi());

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(502, result.StatusCode);
    }

    [Fact]
    public async Task Unreachable_deepl_returns_bad_gateway()
    {
        var (controller, handler) = Create();
        handler.Failure = new HttpRequestException("offline");

        var response = await controller.Translate(Okapi());

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(502, result.StatusCode);
    }

    [Fact]
    public async Task Slow_deepl_returns_gateway_timeout()
    {
        var (controller, handler) = Create();
        handler.Failure = new TaskCanceledException("timeout");

        var response = await controller.Translate(Okapi());

        var result = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(504, result.StatusCode);
    }

    private static HttpRequestMessage Post(string? adminKey, AnimalTexts texts)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/translate") { Content = JsonContent.Create(texts) };
        if (adminKey is not null)
        {
            request.Headers.Add("X-Admin-Key", adminKey);
        }
        return request;
    }

    private static AnimalTexts TooLong() => new() { Profile = new string('a', 3001) };

    [Fact]
    public async Task Request_without_admin_key_returns_unauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.SendAsync(Post(null, Okapi()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_with_wrong_admin_key_returns_unauthorized()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        using var response = await client.SendAsync(Post("wrong-key", Okapi()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task The_eleventh_translation_in_a_minute_gets_429_even_with_the_admin_key()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++)
        {
            using var response = await client.SendAsync(Post(ApiFactory.TestKey, TooLong()));
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(10), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    [Fact]
    public async Task Translate_limit_is_separate_from_the_wikipedia_import_limit()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var import = new HttpRequestMessage(HttpMethod.Get, "/api/wikipedia/import?q=");
            import.Headers.Add("X-Admin-Key", ApiFactory.TestKey);
            using var _ = await client.SendAsync(import);
        }
        using var translate = await client.SendAsync(Post(ApiFactory.TestKey, TooLong()));

        Assert.Equal(HttpStatusCode.BadRequest, translate.StatusCode);
    }
}