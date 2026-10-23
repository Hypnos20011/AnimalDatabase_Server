using AnimalDatabase.Api.Services;
using static AnimalDatabase.Api.Services.WikipediaText;

namespace AnimalDatabase.Api.Tests;

public class WikipediaTextTests
{
    private static (decimal? Min, decimal? Max)? Measure(string text, Quantity quantity) =>
        FindMeasurement(new List<Section> { new(string.Empty, text) }, quantity);

    [Theory]
    [InlineData("Adults measure 30–40 cm (12–16 in) in length.", 30, 40)]
    [InlineData("The body is 8 to 12 mm long.", 0.8, 1.2)]
    [InlineData("Adults are 1.5 to 2 m long.", 150, 200)]
    [InlineData("Adults are 10 to 20 inches long.", 25.4, 50.8)]
    [InlineData("Adults grow 3 to 4 ft in length.", 91.44, 121.92)]
    public void Lengths_are_converted_to_centimetres(string text, double min, double max)
    {
        var result = Measure(text, Quantity.Length);

        Assert.NotNull(result);
        Assert.Equal((decimal)min, result.Value.Min!.Value, 2);
        Assert.Equal((decimal)max, result.Value.Max!.Value, 2);
    }

    [Theory]
    [InlineData("Adults weigh 200 to 300 g.", 0.2, 0.3)]
    [InlineData("Adults weigh 4–7 kg.", 4, 7)]
    [InlineData("Adults weigh between 10 and 20 lb.", 4.54, 9.07)]
    public void Weights_are_converted_to_kilograms(string text, double min, double max)
    {
        var result = Measure(text, Quantity.Weight);

        Assert.NotNull(result);
        Assert.Equal((decimal)min, result.Value.Min!.Value, 2);
        Assert.Equal((decimal)max, result.Value.Max!.Value, 2);
    }

    [Fact]
    public void An_upper_bound_only_gives_a_maximum()
    {
        var result = Measure("Adults can weigh up to 5 kg.", Quantity.Weight);

        Assert.NotNull(result);
        Assert.Null(result.Value.Min);
        Assert.Equal(5m, result.Value.Max);
    }

    [Fact]
    public void A_lower_bound_only_gives_a_minimum()
    {
        var result = Measure("In captivity they can live for more than 20 years.", Quantity.Lifespan);

        Assert.NotNull(result);
        Assert.Equal(20m, result.Value.Min);
        Assert.Null(result.Value.Max);
    }

    [Fact]
    public void A_lifespan_range_is_read_in_years()
    {
        var result = Measure("The life expectancy of a wild axolotl is between 10 and 15 years.", Quantity.Lifespan);

        Assert.NotNull(result);
        Assert.Equal(10m, result.Value.Min);
        Assert.Equal(15m, result.Value.Max);
    }

    [Theory]
    [InlineData("At birth, the young weigh 100 g.", "Weight")]
    [InlineData("Hatchlings measure 5 cm in length.", "Length")]
    [InlineData("Newborns weigh about 2 kg.", "Weight")]
    [InlineData("The eggs are 3 cm long.", "Length")]
    [InlineData("The tadpoles live for 6 months.", "Lifespan")]
    public void Birth_and_young_values_are_ignored(string text, string quantity)
    {
        Assert.Null(Measure(text, Enum.Parse<Quantity>(quantity)));
    }

    [Fact]
    public void A_single_large_specimen_does_not_widen_the_adult_range()
    {
        var result = Measure(
            "Adults are usually between 3 and 4 m long, although it can grow significantly larger—such as one specimen captured in 2000, which was believed to measure around 6 meters.",
            Quantity.Length);

        Assert.NotNull(result);
        Assert.Equal(300m, result.Value.Min);
        Assert.Equal(400m, result.Value.Max);
    }

    [Fact]
    public void Sentences_without_a_size_word_give_nothing()
    {
        Assert.Null(Measure("It was seen 30 km from the coast.", Quantity.Length));
    }

    [Theory]
    [InlineData("The okapi is shy.<ref name=\"a\">Smith 2000</ref>", "The okapi is shy.")]
    [InlineData("The okapi is shy.<ref name=\"b\" />[3]", "The okapi is shy.")]
    [InlineData("It lives in [[Democratic Republic of the Congo|DR Congo]].{{sfn|Smith|2000}}", "It lives in DR Congo.")]
    [InlineData("It is rare.[citation needed]", "It is rare.")]
    [InlineData("It eats <!-- hidden -->leaves.", "It eats leaves.")]
    [InlineData("It is '''large''' and ''shy''.", "It is large and shy.")]
    public void Citations_and_markup_are_stripped(string input, string expected)
    {
        Assert.Equal(expected, StripCitations(input).Trim());
    }

    [Theory]
    [InlineData("the Amazon basin[2]", "Amazon basin")]
    [InlineData("forests (mostly)", null)]
    [InlineData("areas where it", null)]
    [InlineData("x", null)]
    public void Phrases_are_cleaned_or_rejected(string input, string? expected)
    {
        Assert.Equal(expected, CleanPhrase(input));
    }

    [Fact]
    public void Habitat_and_range_are_split_from_one_sentence()
    {
        var sections = new List<Section> { new(string.Empty, "The pink fairy armadillo is endemic to the deserts and scrub lands of central Argentina.") };

        var (habitat, range) = FindHabitatAndRange(sections);

        Assert.Equal("Deserts and scrub lands", habitat);
        Assert.Equal("Central Argentina", range);
    }

    [Fact]
    public void Historic_ranges_are_skipped()
    {
        var sections = new List<Section> { new(string.Empty, "It was formerly found in Europe.") };

        var (_, range) = FindHabitatAndRange(sections);

        Assert.Null(range);
    }

    [Fact]
    public void Infobox_status_family_and_caption_are_read()
    {
        const string wikitext = "{{Automatic taxobox\n| status = VU\n| status_system = IUCN3.1\n| status_ref = <ref>{{cite iucn|year=2020}}</ref>\n| taxon = Balaeniceps rex\n| familia = [[Balaenicipitidae]]\n| range_map_caption = Swamps of [[East Africa]]\n}}\nThe '''shoebill''' is a bird.";

        var infobox = ReadInfobox(wikitext);

        Assert.Equal("VU", infobox.Status);
        Assert.Equal("IUCN3.1", infobox.StatusSystem);
        Assert.Equal("Balaenicipitidae", infobox.Family);
        Assert.Equal("Swamps of East Africa", infobox.RangeMapCaption);
    }

    [Fact]
    public void Bold_lead_names_skip_the_italic_latin_name()
    {
        const string wikitext = "{{Speciesbox}}\n'''''Hyalinobatrachium fleischmanni''''', also known as '''Fleischmann's glass frog''' or the '''northern glass frog''', is a frog.";

        Assert.Equal(new[] { "Fleischmann's glass frog", "northern glass frog" }, BoldLeadNames(wikitext));
    }

    [Fact]
    public void Profile_uses_the_first_two_lead_sentences()
    {
        var profile = ProfileFromLead("The okapi is a mammal. It is the only species in the genus Okapia. It lives in forests.");

        Assert.Equal("The okapi is a mammal. It is the only species in the genus Okapia.", profile);
    }

    [Fact]
    public void Fun_fact_never_repeats_a_profile_sentence()
    {
        const string lead = "The okapi is the only living relative of the giraffe. It is shy.";
        var sections = new List<Section>
        {
            new(string.Empty, lead),
            new("Description", "Okapis are the only known mammals that can clean their ears with their long tongues."),
        };

        var fact = FindFunFact(sections, lead);

        Assert.Equal("Okapis are the only known mammals that can clean their ears with their long tongues.", fact);
    }

    [Fact]
    public void Conservation_sentences_are_not_fun_facts()
    {
        const string lead = "The goblin shark is a shark. It is rare.";
        var sections = new List<Section>
        {
            new(string.Empty, lead),
            new("Description", "The IUCN has assessed it as Least Concern, as it is the only shark that is rarely caught."),
        };

        Assert.Null(FindFunFact(sections, lead));
    }

    [Theory]
    [InlineData("The octopus tends to have a lifespan around two to three years.", 2, 3)]
    [InlineData("In the wild they live for about twelve years.", 12, 12)]
    [InlineData("Their life expectancy is between ten and fifteen years.", 10, 15)]
    public void Lifespans_written_in_words_are_read(string text, double min, double max)
    {
        var result = Measure(text, Quantity.Lifespan);

        Assert.NotNull(result);
        Assert.Equal((decimal)min, result.Value.Min);
        Assert.Equal((decimal)max, result.Value.Max);
    }

    [Theory]
    [InlineData("A twenty-five year study followed the colony.")]
    [InlineData("They form groups of two to three animals.")]
    [InlineData("The ten-year-old specimen was found in a cave.")]
    public void Number_words_without_years_give_no_lifespan(string text)
    {
        Assert.Null(Measure(text, Quantity.Lifespan));
    }

    [Fact]
    public void Number_words_are_not_read_as_lengths()
    {
        Assert.Null(Measure("Adults grow two to three metres long.", Quantity.Length));
    }

    [Fact]
    public void A_negated_lower_bound_gives_a_maximum()
    {
        var result = Measure("In the wild, capybaras usually do not live longer than four years because of predation.", Quantity.Lifespan);

        Assert.NotNull(result);
        Assert.Null(result.Value.Min);
        Assert.Equal(4m, result.Value.Max);
    }

    [Fact]
    public void Range_is_read_from_the_end_of_a_long_habitat_phrase()
    {
        var sections = new List<Section> { new(string.Empty, "Blue-ringed octopuses are four extremely venomous species of octopus that can be found in shells between rocks and crevices in tide pools and also coral reefs in the Pacific and Indian Oceans, from Japan to Australia.") };

        var (habitat, range) = FindHabitatAndRange(sections);

        Assert.Equal("Tide pools and coral reefs", habitat);
        Assert.Equal("Pacific and Indian Oceans, from Japan to Australia", range);
    }
}
