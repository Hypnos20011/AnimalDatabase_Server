using AnimalDatabase.Api.Services;


namespace AnimalDatabase.Api.Tests;

public class EolClientTests
{
    private static (EolClient Client, FixtureHttpHandler Handler) Create(){
        var handler = new FixtureHttpHandler();
        return (new EolClient(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task Finds_range_text_for_spider_crab()
    {
        var (client, handler) = Create();
        var text = await client.FindRangeTextAsync("Macrocheira kaempferi");
        
        Assert.NotNull(text);
        Assert.Contains("Range mass", text);
    }

    [Fact]
    public async Task Returns_null_when_eol_has_no_match()
    {
        var (client, handler) = Create();
        handler.Override("eol-search/Nonexistus_animalus", "{\"results\":[]}");
        var text = await client.FindRangeTextAsync("Nonexistus animalus");
        
        Assert.Null(text);
    }
}