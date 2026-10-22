using AnimalDatabase.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace AnimalDatabase.Api.Tests;

public static class TestImporter
{
    public static (WikipediaImporter Importer, FixtureHttpHandler Handler) Create()
    {
        var handler = new FixtureHttpHandler();
        var importer = new WikipediaImporter(new HttpClient(handler), NullLogger<WikipediaImporter>.Instance);
        return (importer, handler);
    }
}
