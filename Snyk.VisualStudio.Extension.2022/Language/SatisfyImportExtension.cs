using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Snyk.VisualStudio.Extension.Language
{
    public static class SatisfyImportExtension
    {
        private static IComponentModel _compositionService;

        public static void SatisfyImportsOnce(this object o)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                _compositionService ??= ServiceProvider.GlobalProvider.GetService(typeof(SComponentModel))
                    as IComponentModel;

                _compositionService?.DefaultCompositionService.SatisfyImportsOnce(o);
            }
            catch
            {
                /// do nothing
            }
            
        }
    }
}
