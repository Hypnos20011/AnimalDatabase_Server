using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;

namespace AnimalDatabase.Api.Tests;

public class TranslationGapsTests
{
    [Fact]
    public void Missing_only_when_english_has_text_and_the_translation_is_blank()
    {
        var english = new AnimalTexts
        {
            CommonName = "Okapi",
            Habitat = "   ",
            RangeRegion = "Central Africa",
            Profile = "A forest giraffe."
        };
        var german = new AnimalTexts
        {
            CommonName = " ",
            Habitat = null,
            RangeRegion = "Zentralafrika",
            Profile = "Eine Waldgiraffe."
        };

        Assert.Equal(["commonName"], TranslationGaps.Missing(english, german).ToArray());
        Assert.Equal(
            ["commonName", "rangeRegion", "profile"],
            TranslationGaps.Missing(english, null).ToArray());
    }

    [Fact]
    public void Reports_omit_animals_with_no_gaps()
    {
        var english = new AnimalTexts
        {
            CommonName = "Axolotl",
            Habitat = "Lakes",
            RangeRegion = "Mexico",
            Profile = "Stays young."
        };
        var complete = new EnglishAnimalRecord
        {
            English = Animal(1, "Okapi", english),
            German = Copy(english),
            French = Copy(english)
        };
        var nothingToTranslate = new EnglishAnimalRecord
        {
            English = Animal(2, "", new AnimalTexts { Habitat = "  " })
        };
        var missingFrenchProfile = new EnglishAnimalRecord
        {
            English = Animal(3, "Axolotl", english),
            German = Copy(english),
            French = new AnimalTexts
            {
                CommonName = "Axolotl",
                Habitat = "Lacs",
                RangeRegion = "Mexique"
            }
        };

        var reports = TranslationGaps.Reports([complete, nothingToTranslate, missingFrenchProfile]);

        var report = Assert.Single(reports);
        Assert.Equal(3, report.Id);
        Assert.Equal("Axolotl", report.CommonName);
        var language = Assert.Single(report.Languages);
        Assert.Equal("fr", language.Language);
        Assert.Equal(["profile"], language.Fields.ToArray());
    }

    [Fact]
    public void Reports_list_german_then_french_and_only_the_blank_fields()
    {
        var record = new EnglishAnimalRecord
        {
            English = Animal(4, "Okapi", new AnimalTexts
            {
                CommonName = "Okapi",
                Habitat = "Rainforest",
                Profile = "A forest giraffe."
            }),
            German = new AnimalTexts { CommonName = "Okapi" }
        };

        var report = Assert.Single(TranslationGaps.Reports([record]));

        Assert.Equal(["de", "fr"], report.Languages.Select(language => language.Language).ToArray());
        Assert.Equal(["habitat", "profile"], report.Languages[0].Fields.ToArray());
        Assert.Equal(["commonName", "habitat", "profile"], report.Languages[1].Fields.ToArray());
    }

    [Fact]
    public void Pick_copies_only_the_missing_fields()
    {
        var english = new AnimalTexts
        {
            CommonName = "Okapi",
            Habitat = "Rainforest",
            RangeRegion = "Central Africa",
            Profile = "A forest giraffe."
        };

        var picked = TranslationGaps.Pick(english, ["habitat", "profile"]);

        Assert.Null(picked.CommonName);
        Assert.Equal("Rainforest", picked.Habitat);
        Assert.Null(picked.RangeRegion);
        Assert.Equal("A forest giraffe.", picked.Profile);
    }

    [Fact]
    public void Overlay_writes_translated_values_only_onto_the_missing_fields()
    {
        var existing = new AnimalTexts
        {
            CommonName = "Okapi",
            Habitat = "Regenwald",
            RangeRegion = "Zentralafrika",
            Profile = "Alt"
        };
        var translated = new AnimalTexts
        {
            CommonName = "DE:Okapi",
            Habitat = "DE:Rainforest",
            RangeRegion = "DE:Central Africa",
            Profile = "DE:A forest giraffe. Fun fact: DE:Its tongue is very long."
        };

        var saved = TranslationGaps.Overlay(existing, translated, ["profile"]);

        Assert.Equal("Okapi", saved.CommonName);
        Assert.Equal("Regenwald", saved.Habitat);
        Assert.Equal("Zentralafrika", saved.RangeRegion);
        Assert.Equal("DE:A forest giraffe. Fun fact: DE:Its tongue is very long.", saved.Profile);

        var inserted = TranslationGaps.Overlay(null, translated, ["commonName", "profile"]);
        Assert.Equal("DE:Okapi", inserted.CommonName);
        Assert.Null(inserted.Habitat);
        Assert.Null(inserted.RangeRegion);
        Assert.Contains("Fun fact:", inserted.Profile);
    }

    [Fact]
    public void Languages_are_german_and_french()
    {
        Assert.Equal(["de", "fr"], TranslationGaps.Languages.ToArray());
    }

    [Fact]
    public void Text_longer_than_3000_characters_is_too_long_to_send()
    {
        Assert.False(TranslationGaps.TooLong(new AnimalTexts { Profile = new string('a', TranslationGaps.MaxCharacters) }));
        Assert.True(TranslationGaps.TooLong(new AnimalTexts { Profile = new string('a', TranslationGaps.MaxCharacters + 1) }));
    }

    private static EnglishAnimal Animal(int id, string commonName, AnimalTexts texts) => new()
    {
        Id = id,
        CommonName = string.IsNullOrEmpty(commonName) ? texts.CommonName : commonName,
        Habitat = texts.Habitat,
        RangeRegion = texts.RangeRegion,
        Profile = texts.Profile
    };

    private static AnimalTexts Copy(AnimalTexts texts) => new()
    {
        CommonName = texts.CommonName,
        Habitat = texts.Habitat,
        RangeRegion = texts.RangeRegion,
        Profile = texts.Profile
    };
}
