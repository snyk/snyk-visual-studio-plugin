using System;

namespace Snyk.VisualStudio.Extension.Language
{
    public class SnykLanguageServerEventArgs : EventArgs
    {
        public bool IsReady { get; set; }
    }
}
