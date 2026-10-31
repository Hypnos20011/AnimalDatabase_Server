namespace AnimalDatabase.Api.Hosting;

public sealed class SpaSettings
{
    public const string SectionName = "Spa";

    public bool Enabled { get; set; } = true;
    public string RootPath { get; set; } = "wwwroot";
}