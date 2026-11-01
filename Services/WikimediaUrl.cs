using System.Text.RegularExpressions;

namespace AnimalDatabase.Api.Services;

public static class WikimediaUrl
{
    public static string Normalize(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        if (((uri.Host != "upload.wikimedia.org") && (uri.Host != "thumb.wikimedia.org")) || (!uri.AbsolutePath.Contains("/thumb/")))
        {
           return url;
        }

        var newPath = Regex.Replace(uri.AbsolutePath, @"/\d+px-", "/1280px-");
        return $"{uri.Scheme}://{uri.Host}{newPath}";
    }
}