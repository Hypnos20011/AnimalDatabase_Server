using System.Net;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class FallbackBatchTranslatorTests
{
    private sealed class StubBatchTranslator : IBatchTranslator
    {
        private readonly Func<string[], Task<string[]>> _impl;

        public StubBatchTranslator(Func<string[], Task<string[]>> impl)
        {
            _impl = impl;
        }

        public int Calls { get; private set; }
        public string[]? LastTexts { get; private set; }
        public string? LastTarget { get; private set; }

        public Task<string[]> TranslateAsync(string[] texts, string targetLanguage = "de")
        {
            Calls++;
            LastTexts = texts;
            LastTarget = targetLanguage;
            return _impl(texts);
        }
    }

    [Fact]
    public async Task Primary_quota_error_calls_fallback()
    {
        var primary = new StubBatchTranslator(_ => throw new HttpRequestException("quota", null, (HttpStatusCode)456));
        var fallback = new StubBatchTranslator(texts => Task.FromResult(texts.Select(t => "FB:" + t).ToArray()));
        var translator = new FallbackBatchTranslator(primary, fallback);

        var result = await translator.TranslateAsync(["Hello", "World"]);

        Assert.Equal(1, primary.Calls);
        Assert.Equal(1, fallback.Calls);
        Assert.Equal(["Hello", "World"], fallback.LastTexts!);
        Assert.Equal(["FB:Hello", "FB:World"], result);
    }

    [Fact]
    public async Task Primary_success_never_calls_fallback()
    {
        var primary = new StubBatchTranslator(texts => Task.FromResult(texts.Select(t => "OK:" + t).ToArray()));
        var fallback = new StubBatchTranslator(_ => throw new InvalidOperationException("fallback should not run"));
        var translator = new FallbackBatchTranslator(primary, fallback);

        var result = await translator.TranslateAsync(["Hello"]);

        Assert.Equal(1, primary.Calls);
        Assert.Equal(0, fallback.Calls);
        Assert.Equal(["OK:Hello"], result);
    }

    [Fact]
    public async Task Primary_bad_request_does_not_call_fallback()
    {
        var primary = new StubBatchTranslator(_ => throw new HttpRequestException("bad", null, HttpStatusCode.BadRequest));
        var fallback = new StubBatchTranslator(_ => Task.FromResult(new[] { "should-not-run" }));
        var translator = new FallbackBatchTranslator(primary, fallback);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal(1, primary.Calls);
        Assert.Equal(0, fallback.Calls);
    }

    [Fact]
    public async Task Both_fail_throws_the_fallback_error()
    {
        var primary = new StubBatchTranslator(_ => throw new HttpRequestException("quota", null, (HttpStatusCode)456));
        var fallback = new StubBatchTranslator(_ => throw new HttpRequestException("down", null, HttpStatusCode.BadGateway));
        var translator = new FallbackBatchTranslator(primary, fallback);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));

        Assert.Equal(HttpStatusCode.BadGateway, error.StatusCode);
        Assert.Equal(1, primary.Calls);
        Assert.Equal(1, fallback.Calls);
    }

    [Fact]
    public async Task Null_fallback_rethrows_primary_error()
    {
        var primary = new StubBatchTranslator(_ => throw new HttpRequestException("quota", null, (HttpStatusCode)456));
        var translator = new FallbackBatchTranslator(primary, null);

        var error = await Assert.ThrowsAsync<HttpRequestException>(() => translator.TranslateAsync(["Hello"]));

        Assert.Equal((HttpStatusCode)456, error.StatusCode);
        Assert.Equal(1, primary.Calls);
    }

    [Fact]
    public async Task French_target_is_passed_to_primary_and_fallback()
    {
        var primary = new StubBatchTranslator(_ => throw new HttpRequestException("quota", null, (HttpStatusCode)456));
        var fallback = new StubBatchTranslator(texts => Task.FromResult(texts.Select(t => "FB:" + t).ToArray()));
        var translator = new FallbackBatchTranslator(primary, fallback);

        var result = await translator.TranslateAsync(["Hello"], "fr");

        Assert.Equal(1, primary.Calls);
        Assert.Equal(1, fallback.Calls);
        Assert.Equal("fr", primary.LastTarget);
        Assert.Equal("fr", fallback.LastTarget);
        Assert.Equal(["Hello"], fallback.LastTexts!);
        Assert.Equal(["FB:Hello"], result);
    }
}