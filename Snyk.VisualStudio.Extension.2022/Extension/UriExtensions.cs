using System;

namespace Snyk.VisualStudio.Extension.Extension;

public static class UriExtensions
{
    public static string UncAwareAbsolutePath(this Uri uri)
    {
        if (uri == null) return string.Empty;
        return uri.IsUnc ? uri.LocalPath : uri.AbsolutePath;
    }
}