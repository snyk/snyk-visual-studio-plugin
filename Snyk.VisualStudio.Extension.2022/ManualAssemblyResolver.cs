using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Snyk.VisualStudio.Extension
{
    /// <summary>
    /// AssemblyResolver or Assembly loader for load dll's on VS2015 and VS2022.
    /// </summary>
    public class ManualAssemblyResolver : IDisposable
    {
        private static ManualAssemblyResolver instance;

        private readonly IEnumerable<Assembly> assemblies;

        private ManualAssemblyResolver(IEnumerable<Assembly> assemblies)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException("assemblies");
            }

            if (assemblies.Count() == 0)
            {
                throw new ArgumentException("Assemblies should be not empty.", "assemblies");
            }

            this.assemblies = assemblies;

            AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
        }

        /// <summary>
        /// Initialize <see cref="ManualAssemblyResolver"/> instance.
        /// </summary>
        /// <param name="assembliesPath">Path to extension installation folder.</param>
        public static void Initialize(string assembliesPath)
        {
            string[] files = Directory.GetFileSystemEntries(assembliesPath, "*.dll", SearchOption.AllDirectories);

            var assemblies = new List<Assembly>();

            foreach (string filePath in files)
            {
                assemblies.Add(Assembly.LoadFrom(filePath));
            }

            instance = new ManualAssemblyResolver(assemblies);
        }

        /// <summary>
        /// Remove event listener from current domain.
        /// </summary>
        public void Dispose() => AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string requestedName = args.Name.Substring(0, args.Name.IndexOf(","));

            return this.FindAssembly(requestedName);
        }

        private Assembly FindAssembly(string requestedName)
        {
            foreach (Assembly assembly in this.assemblies)
            {
                if (requestedName == assembly.GetName().Name)
                {
                    return assembly;
                }
            }

            return null;
        }
    }
}
