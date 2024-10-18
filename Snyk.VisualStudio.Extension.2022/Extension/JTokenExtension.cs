using System;
using Newtonsoft.Json.Linq;

namespace Snyk.VisualStudio.Extension.Extension
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
