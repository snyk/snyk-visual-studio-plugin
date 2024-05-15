using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel.Composition;

namespace Snyk.VisualStudio.Extension.Shared.Extension
{
    public static class SatisfyImportExtension
    {
        private static IComponentModel _compositionService;

        public static void SatisfyImportsOnce(this object o)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            _compositionService ??= ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel)) 
                    as IComponentModel;

            _compositionService?.DefaultCompositionService.SatisfyImportsOnce(o);
        }
    }
}
