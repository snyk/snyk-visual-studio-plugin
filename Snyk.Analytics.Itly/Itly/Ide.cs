using System.Collections.Generic;

namespace Iteratively
{
    public enum Ide
    {
        VisualStudioCode,
        VisualStudio,
        Eclipse,
        JetBrains
    }

    public static class IdeExtensions
    {
        private static readonly Dictionary<Ide, string> IdeValues = new Dictionary<Ide, string>
        {
            [Ide.VisualStudioCode] = "Visual Studio Code",
            [Ide.VisualStudio] = "Visual Studio",
            [Ide.Eclipse] = "Eclipse",
            [Ide.JetBrains] = "JetBrains"
        };

        public static string ToString(this Ide ide) => IdeValues[ide];
    }
}