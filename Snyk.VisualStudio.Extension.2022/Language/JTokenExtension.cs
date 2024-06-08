using Newtonsoft.Json.Linq;
using System;

namespace Snyk.VisualStudio.Extension.Shared.Language
{
    public static class JTokenExtension
    {
        public static T TryParse<T>(this JToken arg) where T : class
        {
            T res;
            try
            {
                res = arg.ToObject<T>();
            }
            catch (Exception)
            {
                return default;
            }
            return res;
        }
    }
}
