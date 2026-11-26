using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;
using AnimalDatabase.Api.Services;
using Microsoft.Extensions.Logging;

namespace AnimalDatabase.Api.Tests;

public class MissingTranslationFillerTests
{
    [Fact]
    public async Task Does_not_translate_when_the_english_record_is_unavailable()
    {
        var store = new MemoryStore();
        var translator = new PrefixTranslator();
        var filler = Create(store, translator);

        await filler.FillAsync(5);

        Assert.Equal(0, translator.Calls);
        Assert.Empty(store.Saves);
    }

    [Fact]
    public async Task Keeps_an_existing_german_name_while_filling_the_profile()
    {
        var store = new MemoryStore
        {
            English = new EnglishAnimal
            {
                Id = 5,
                CommonName = "Okapi",
                Habitat = "Rainforest",
                RangeRegion = "Central Africa",
                Profile = "A forest giraffe. Fun fact: Its tongue is very long."
            }
        };
        store.Translations["de"] = new AnimalTexts
        {
            CommonName = "Okapi",
            Habitat = "Regenwald",
            RangeRegion = "Zentralafrika"
        };
        var translator = new PrefixTranslator();
        var filler = Create(store, translator);

        await filler.FillAsync(5);

        var german = Assert.Single(store.Saves, save => save.Language == "de");
        Assert.Equal("Okapi", german.Texts.CommonName);
        Assert.Equal("Regenwald", german.Texts.Habitat);
        Assert.Equal("Zentralafrika", german.Texts.RangeRegion);
        Assert.Equal("DE:A forest giraffe. Fun fact: DE:Its tongue is very long.", german.Texts.Profile);
        Assert.Contains("Fun fact:", german.Texts.Profile);
        Assert.Equal(["A forest giraffe.", "Its tongue is very long."], translator.Requests.Single(request => request.Target == "de").Texts);
        Assert.Equal("Okapi", store.Translations["de"].CommonName);
        Assert.Contains("Fun fact:", store.Translations["de"].Profile);
    }

    [Fact]
    public async Task Does_not_call_the_translator_when_nothing_is_missing()
    {
        var store = new MemoryStore
        {
            English = new EnglishAnimal
            {
                Id = 5,
                CommonName = "Okapi",
                Habitat = "Rainforest",
                RangeRegion = "Central Africa",
                Profile = "A forest giraffe."
            }
        };
        var filled = new AnimalTexts
        {
            CommonName = "Okapi",
            Habitat = "Regenwald",
            RangeRegion = "Zentralafrika",
            Profile = "Eine Waldgiraffe."
        };
        store.Translations["de"] = filled;
        store.Translations["fr"] = new AnimalTexts
        {
            CommonName = "Okapi",
            Habitat = "Forêt",
            RangeRegion = "Afrique centrale",
            Profile = "Une girafe."
        };
        var translator = new PrefixTranslator();
        var filler = Create(store, translator);

        await filler.FillAsync(5);

        Assert.Equal(0, translator.Calls);
        Assert.Empty(store.Saves);
        Assert.Same(filled, store.Translations["de"]);
    }

    [Fact]
    public async Task A_throwing_translator_does_not_escape_or_erase_existing_text()
    {
        var store = new MemoryStore
        {
            English = new EnglishAnimal
            {
                Id = 5,
                CommonName = "Okapi",
                Habitat = "Rainforest",
                Profile = "A forest giraffe. Fun fact: Its tongue is very long."
            }
        };
        var german = new AnimalTexts { CommonName = "Okapi", Habitat = "Regenwald" };
        var french = new AnimalTexts { CommonName = "Okapi", Habitat = "Forêt" };
        store.Translations["de"] = german;
        store.Translations["fr"] = french;
        var logger = new WarningLogger();
        var filler = Create(store, new ThrowingTranslator(), logger);

        var exception = await Record.ExceptionAsync(() => filler.FillAsync(5));

        Assert.Null(exception);
        Assert.Same(german, store.Translations["de"]);
        Assert.Same(french, store.Translations["fr"]);
        Assert.Equal("Okapi", german.CommonName);
        Assert.Equal("Regenwald", german.Habitat);
        Assert.Null(german.Profile);
        Assert.Equal("Okapi", french.CommonName);
        Assert.Empty(store.Saves);
        Assert.NotEmpty(logger.Warnings);
    }

    [Fact]
    public async Task Skips_a_language_when_its_missing_text_is_longer_than_3000_characters()
    {
        var store = new MemoryStore
        {
            English = new EnglishAnimal
            {
                Id = 1,
                CommonName = "Okapi",
                Profile = new string('p', 3001)
            }
        };
        store.Translations["de"] = new AnimalTexts { Profile = "Schon da" };
        var translator = new PrefixTranslator();
        var filler = Create(store, translator);

        await filler.FillAsync(1);

        var german = Assert.Single(store.Saves);
        Assert.Equal("de", german.Language);
        Assert.Equal("DE:Okapi", german.Texts.CommonName);
        Assert.Equal("Schon da", german.Texts.Profile);
        Assert.Equal(["de"], translator.Requests.Select(request => request.Target).ToArray());
    }

    [Fact]
    public async Task Report_is_null_when_the_animal_does_not_exist_and_lists_gaps_otherwise()
    {
        var store = new MemoryStore
        {
            English = new EnglishAnimal { Id = 2, CommonName = "Okapi", Profile = "A forest giraffe." }
        };
        var filler = Create(store, new PrefixTranslator());

        Assert.Null(await filler.ReportAsync(9));
        var report = await filler.ReportAsync(2);

        Assert.NotNull(report);
        Assert.Equal(2, report.Id);
        Assert.Equal("Okapi", report.CommonName);
        Assert.Equal(["de", "fr"], report.Languages.Select(language => language.Language).ToArray());
        Assert.All(report.Languages, language => Assert.Equal(["commonName", "profile"], language.Fields.ToArray()));
    }

    private static MissingTranslationFiller Create(
        IAnimalTranslationStore store,
        IBatchTranslator translator,
        ILogger<MissingTranslationFiller>? logger = null)
    {
        return new MissingTranslationFiller(store, new AnimalTextTranslator(translator), logger ?? new WarningLogger());
    }

    private sealed class MemoryStore : IAnimalTranslationStore
    {
        public EnglishAnimal? English { get; set; }
        public Dictionary<string, AnimalTexts> Translations { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<(string Language, AnimalTexts Texts)> Saves { get; } = [];

        public Task<AnimalTexts?> GetTranslationAsync(int id, string language) =>
            Task.FromResult(Translations.TryGetValue(language, out var texts) ? texts : null);

        public Task<bool> SaveTranslationAsync(int id, string language, AnimalTexts texts)
        {
            Saves.Add((language, texts));
            Translations[language] = texts;
            return Task.FromResult(true);
        }

        public Task<EnglishAnimal?> GetEnglishAsync(int id) =>
            Task.FromResult(English is not null && English.Id == id ? English : null);

        public Task<IReadOnlyList<EnglishAnimalRecord>> ListEnglishAsync() =>
            Task.FromResult<IReadOnlyList<EnglishAnimalRecord>>([]);
    }

    private sealed class PrefixTranslator : IBatchTranslator
    {
        public int Calls { get; private set; }
        public List<(string Target, string[] Texts)> Requests { get; } = [];

        public Task<string[]> TranslateAsync(string[] texts, string targetLanguage = "de")
        {
            Calls++;
            Requests.Add((targetLanguage, texts));
            var prefix = targetLanguage.Equals("fr", StringComparison.OrdinalIgnoreCase) ? "FR:" : "DE:";
            return Task.FromResult(texts.Select(text => prefix + text).ToArray());
        }
    }

    private sealed class ThrowingTranslator : IBatchTranslator
    {
        public Task<string[]> TranslateAsync(string[] texts, string targetLanguage = "de") =>
            throw new InvalidOperationException("translator down");
    }

    private sealed class WarningLogger : ILogger<MissingTranslationFiller>
    {
        public List<Exception> Warnings { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => EmptyDisposable.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning && exception is not null)
            {
                Warnings.Add(exception);
            }
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
