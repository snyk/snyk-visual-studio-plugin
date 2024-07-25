using System;
using System.Windows;

namespace Snyk.VisualStudio.Extension.UI
{
    public static class ResourceLoader
    {
        private static string _resourceBasePath;
        // ReSharper disable InconsistentNaming
        private static readonly string _assemblyName = "Snyk.VisualStudio.Extension";
        private static readonly string _resourcesDirectory = "/Resources";
        // ReSharper restore InconsistentNaming

        public static string GetResourcePath(string imageName)
        {
            var resourceBasePath = GetBaseResourcePath(_assemblyName, _resourcesDirectory, imageName);
            if (string.IsNullOrEmpty(imageName))
                throw new ArgumentNullException(nameof(imageName));
            if (string.IsNullOrEmpty(resourceBasePath))
                throw new ArgumentException($"Image with Path {imageName} not found");
            
            return resourceBasePath + imageName;
        }

        public static string GetBaseResourcePath(string assemblyName, string resourcesDirectory, string imageName)
        {
            if(!string.IsNullOrEmpty(_resourceBasePath))
                return _resourceBasePath;

            var uri = $"pack://application:,,,/{assemblyName};component/{imageName}";
            if (ResourceExists(uri))
            {
                _resourceBasePath =  uri.Replace(imageName, string.Empty);
                return _resourceBasePath;
            }

            var uriWithResources = $"pack://application:,,,/{assemblyName};component{resourcesDirectory}/{imageName}";
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