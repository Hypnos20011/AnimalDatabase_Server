using AnimalDatabase.Api.Data;
using AnimalDatabase.Api.Models;

namespace AnimalDatabase.Api.Services;

public class MissingTranslationFiller
{
    private readonly IAnimalTranslationStore _store;
    private readonly AnimalTextTranslator _translator;
    private readonly ILogger<MissingTranslationFiller> _logger;

    public MissingTranslationFiller(
        IAnimalTranslationStore store,
        AnimalTextTranslator translator,
        ILogger<MissingTranslationFiller> logger)
    {
        _store = store;
        _translator = translator;
        _logger = logger;
    }

    public async Task FillAsync(int id)
    {
        var english = await _store.GetEnglishAsync(id);
        if (english is null)
        {
            return;
        }

        var source = english.ToTexts();
        foreach (var language in TranslationGaps.Languages)
        {
            try
            {
                var existing = await _store.GetTranslationAsync(id, language);
                var missing = TranslationGaps.Missing(source, existing);
                if (missing.Count == 0)
                {
                    continue;
                }

                var picked = TranslationGaps.Pick(source, missing);
                if (TranslationGaps.TooLong(picked))
                {
                    continue;
                }

                var translated = await _translator.TranslateAsync(picked, language);
                var overlay = TranslationGaps.Overlay(existing, translated, missing);
                await _store.SaveTranslationAsync(id, language, overlay);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not fill missing {Language} text for animal {AnimalId}", language, id);
            }
        }
    }

    public async Task<MissingTranslationReport?> ReportAsync(int id)
    {
        var english = await _store.GetEnglishAsync(id);
        if (english is null)
        {
            return null;
        }

        var reports = TranslationGaps.Reports(
        [
            new EnglishAnimalRecord
            {
                English = english,
                German = await _store.GetTranslationAsync(id, "de"),
                French = await _store.GetTranslationAsync(id, "fr")
            }
        ]);
        if (reports.Count == 0)
        {
            return new MissingTranslationReport
            {
                Id = english.Id,
                CommonName = english.CommonName ?? "",
                Languages = []
            };
        }

        return reports[0];
    }
}
