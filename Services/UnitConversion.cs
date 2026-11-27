namespace AnimalDatabase.Api.Services;

public static class UnitConversion
{
    public static decimal CentimetresToMillimetres(decimal centimetres)
    {
        return RoundLengthMillimetres(centimetres * 10m);
    }

    public static decimal MillimetresToMillimetres(decimal millimetres)
    {
        return RoundLengthMillimetres(millimetres);
    }

    public static decimal MetresToMillimetres(decimal metres)
    {
        return RoundLengthMillimetres(metres * 1000m);
    }

    public static decimal InchesToMillimetres(decimal inches)
    {
        return RoundLengthMillimetres(inches * 25.4m);
    }

    public static decimal FeetToMillimetres(decimal feet)
    {
        return RoundLengthMillimetres(feet * 304.8m);
    }

    public static decimal KilogramsToGrams(decimal kilograms)
    {
        return RoundGrams(kilograms * 1000m);
    }

    public static decimal GramsToGrams(decimal grams)
    {
        return RoundGrams(grams);
    }

    public static decimal PoundsToGrams(decimal pounds)
    {
        return RoundGrams(pounds * 453.59237m);
    }

    public static decimal YearsToMonths(decimal years)
    {
        return RoundMonths(years * 12m);
    }

    public static decimal RoundLengthMillimetres(decimal millimetres)
    {
        return Math.Round(millimetres, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal RoundGrams(decimal grams)
    {
        return Math.Round(grams, 3, MidpointRounding.AwayFromZero);
    }

    public static decimal RoundMonths(decimal months)
    {
        return Math.Round(months, 1, MidpointRounding.AwayFromZero);
    }
}