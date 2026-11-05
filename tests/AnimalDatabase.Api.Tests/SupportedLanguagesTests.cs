using AnimalDatabase.Api.Localization;

namespace AnimalDatabase.Api.Tests;

public class SupportedLanguagesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_language_falls_back_to_default(string? language)
    {
        Assert.Equal("en", SupportedLanguages.Normalize(language));
    }

    [Theory]
    [InlineData("it")]
    [InlineData("fra")]
    [InlineData("deu")]
    [InlineData("de-AT")]
    public void Unsupported_language_falls_back_to_default(string language)
    {
        Assert.Equal("en", SupportedLanguages.Normalize(language));
    }

    [Theory]
    [InlineData("en", "en")]
    [InlineData("de", "de")]
    [InlineData("fr", "fr")]
    public void Supported_language_is_kept(string language, string expected)
    {
        Assert.Equal(expected, SupportedLanguages.Normalize(language));
    }

    [Theory]
    [InlineData("DE", "de")]
    [InlineData("En", "en")]
    [InlineData("FR", "fr")]
    public void Supported_language_is_lowercased(string language, string expected)
    {
        Assert.Equal(expected, SupportedLanguages.Normalize(language));
    }

    [Fact]
    public void Default_is_a_supported_language()
    {
        Assert.Contains(SupportedLanguages.Default, SupportedLanguages.All);
    }

    [Fact]
    public void All_contains_exactly_english_german_and_french()
    {
        Assert.Equal(3, SupportedLanguages.All.Count);
        Assert.Contains("en", SupportedLanguages.All);
        Assert.Contains("de", SupportedLanguages.All);
        Assert.Contains("fr", SupportedLanguages.All);
    }
}