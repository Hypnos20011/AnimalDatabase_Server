namespace AnimalDatabase.Api.Services;

public class FallbackBatchTranslator : IBatchTranslator
{
    private readonly IBatchTranslator _primary;
    private readonly IBatchTranslator? _fallback;

    public FallbackBatchTranslator(IBatchTranslator primary, IBatchTranslator? fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public async Task<string[]> TranslateAsync(string[] texts, string targetLanguage = "de")
    {
        try
        {
            return await _primary.TranslateAsync(texts, targetLanguage);
        }
        catch (Exception ex) when (_fallback is not null && IsRecoverable(ex))
        {
            return await _fallback.TranslateAsync(texts, targetLanguage);
        }
    }

    private static bool IsRecoverable(Exception ex)
    {
        if (ex is TaskCanceledException)
        {
            return true;
        }

        if (ex is not HttpRequestException http)
        {
            return false;
        }

        var code = (int?)http.StatusCode;
        switch (code)
        {
            case null:
            case 456:
            case 429:
            case 403:
            case >= 500 and <= 599:
                return true;
            default:
                return false;
        }
    }
}