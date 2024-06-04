using System;
using System.Windows;

namespace Snyk.VisualStudio.Extension
{
    public static class ResourceLoader
    {
        private static string _resourceBasePath;

        public static string GetResourcePath(string imageName)
        {
            var resourceBasePath = GetBaseResourcePath(imageName);
            if (string.IsNullOrEmpty(imageName))
                throw new ArgumentNullException("imageName is null");
            if (string.IsNullOrEmpty(resourceBasePath))
                throw new ArgumentException($"Image with Path {imageName} not found");
            
            return resourceBasePath + imageName;
        }

        public static string GetBaseResourcePath(string imageName)
        {
            if(!string.IsNullOrEmpty(_resourceBasePath))
                return _resourceBasePath;

            var uri = $"pack://application:,,,/Snyk.VisualStudio.Extension;component/{imageName}";
            if (ResourceExists(uri))
            {
                _resourceBasePath =  uri.Replace(imageName, string.Empty);
                return _resourceBasePath;
            }

            var uriWithResources = $"pack://application:,,,/Snyk.VisualStudio.Extension;component/Resources/{imageName}";
            _resourceBasePath =  ResourceExists(uriWithResources) ? uriWithResources.Replace(imageName, string.Empty) : null;
            
            return _resourceBasePath;
        }

        private static bool ResourceExists(string uri)
        {
            try
            {
                var resource = Application.GetResourceStream(new Uri(uri));
                return resource != null;
            }
            catch
            {
                return false;
            }
        }
    }
}