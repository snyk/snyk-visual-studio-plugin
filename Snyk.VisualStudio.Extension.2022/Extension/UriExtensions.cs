using System;

namespace Snyk.VisualStudio.Extension.Extension;

public static class UriExtensions
{
    public static string UncAwareAbsolutePath(this Uri uri)
    {
        if (uri == null) return string.Empty;
        return uri.LocalPath;
    }

    /// <summary>
    /// True when <paramref name="value"/> is an absolute http/https URL. Used to validate
    /// endpoints / links that originate from web content (LS-served HTML, JS bridge args) before
    /// they are persisted into options or handed to the OS, so a page can't repoint the API host
    /// or launch arbitrary URI handlers.
    /// </summary>
    public static bool IsValidWebUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}