using System.Text;
using System.Text.RegularExpressions;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace AnimalDatabase.Api.OpenApi;

public static partial class SwaggerUiAssets
{
    public static string Version { get; } =
        typeof(SwaggerUIOptions).Assembly.GetName().Version?.ToString() ?? "0";

    public static void UseVersionedAssets(this SwaggerUIOptions options)
    {
        options.ScriptBundlePath = WithVersion(options.ScriptBundlePath, Version);
        options.ScriptPresetsPath = WithVersion(options.ScriptPresetsPath, Version);
        options.StylesPath = WithVersion(options.StylesPath, Version);

        var original = options.IndexStream;
        options.IndexStream = () =>
        {
            using var reader = new StreamReader(original(), Encoding.UTF8);
            var html = AddVersion(reader.ReadToEnd(), Version);
            return new MemoryStream(Encoding.UTF8.GetBytes(html));
        };
    }

    public static string WithVersion(string path, string version)
    {
        return path.Contains('?') ? path : $"{path}?v={Uri.EscapeDataString(version)}";
    }

    public static string AddVersion(string html, string version)
    {
        return AssetReference().Replace(html, match =>
            $"{match.Groups["attr"].Value}=\"{WithVersion(match.Groups["path"].Value, version)}\"");
    }

    [GeneratedRegex("(?<attr>src|href)=\"(?<path>(?:\\./)?[A-Za-z0-9_.-]+\\.(?:js|css))\"")]
    private static partial Regex AssetReference();
}
