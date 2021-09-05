namespace Snyk.VisualStudio.Extension.Service
{
    using System;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.TextManager.Interop;

    /// <summary>
    /// Visual Studio service for work with code.
    /// </summary>
    public class VsCodeService
    {
        /// <summary>
        /// Gets instance of this class.
        /// </summary>
        public static VsCodeService Instance { get; private set; }

        /// <summary>
        /// Initialize <see cref="VsCodeService"/> instance.
        /// </summary>
        public static void Initialize()
        {
            Instance = new VsCodeService();
        }

        /// <summary>
        /// Open file in editor and navigate to text by row and column.
        /// </summary>
        /// <param name="documentFullPath">File path.</param>
        /// <param name="startLine">Start line (row).</param>
        /// <param name="startColumn">Start column.</param>
        /// <param name="endLine">End line (row).</param>
        /// <param name="endColumn">End column.</param>
        public void OpenAndNavigate(string documentFullPath, int startLine, int startColumn, int endLine, int endColumn)
        {
            if (documentFullPath == null)
            {
                throw new ArgumentNullException(nameof(documentFullPath));
            }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var shellOpenDocument = AsyncPackage.GetGlobalService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

                IVsWindowFrame windowFrame;
                Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider;
                IVsUIHierarchy vsUiHierarchy;
                uint itemId;
                Guid logicalView = VSConstants.LOGVIEWID_Code;

                if (ErrorHandler.Failed(shellOpenDocument.OpenDocumentViaProject(documentFullPath, ref logicalView, out serviceProvider, out vsUiHierarchy, out itemId, out windowFrame)) || windowFrame == null)
                {
                    return;
                }

                object documentData;
                windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out documentData);

                var textBuffer = documentData as VsTextBuffer;

                if (textBuffer == null)
                {
                    IVsTextBufferProvider bufferProvider = documentData as IVsTextBufferProvider;
                    if (bufferProvider != null)
                    {
                        IVsTextLines vsTextLines;

                        ErrorHandler.ThrowOnFailure(bufferProvider.GetTextBuffer(out vsTextLines));

                        textBuffer = vsTextLines as VsTextBuffer;

                        if (textBuffer == null)
                        {
                            return;
                        }
                    }
                }

                IVsTextManager textManager = Package.GetGlobalService(typeof(VsTextManagerClass)) as IVsTextManager;

                textManager.NavigateToLineAndColumn(textBuffer, VSConstants.LOGVIEWID_TextView, startLine, startColumn, endLine, endColumn);
            });
        }
    }
}
