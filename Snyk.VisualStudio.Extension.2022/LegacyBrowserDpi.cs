using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Forms;

namespace Snyk.VisualStudio.Extension
{
        // WinForms WebBrowser allows overriding the "site" to provide IDocHostUIHandler.
        public sealed class DpiAwareWebBrowser : WebBrowser
        {
            protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
                => new DpiAwareSite(this);

            private sealed class DpiAwareSite : WebBrowserSite, NativeMethods.IDocHostUIHandler
            {
                public DpiAwareSite(WebBrowser host) : base(host) { }

                public int GetHostInfo(ref NativeMethods.DOCHOSTUIINFO info)
                {
                    // Required by MSHTML: fill cbSize
                    info.cbSize = Marshal.SizeOf(typeof(NativeMethods.DOCHOSTUIINFO));

                    // Key line: equivalent behavior to FEATURE_96DPI_PIXEL but per-host
                    info.dwFlags |= NativeMethods.DOCHOSTUIFLAG.DPI_AWARE;

                    return NativeMethods.S_OK;
                }

                // --- Required interface members (mostly defaults) ---
                public int ShowContextMenu(int dwID, IntPtr pt, IntPtr pcmdtReserved, IntPtr pdispReserved) => NativeMethods.S_FALSE;
                public int GetUIContext(IntPtr ppunk) => NativeMethods.E_NOTIMPL;
                public int TranslateUrl(int dwTranslate, string strURLIn, out string pstrURLOut) { pstrURLOut = null; return NativeMethods.E_NOTIMPL; }
                public int FilterDataObject(IntPtr pDO, out IntPtr ppDORet) { ppDORet = IntPtr.Zero; return NativeMethods.E_NOTIMPL; }
                public int TranslateAccelerator(ref NativeMethods.MSG lpmsg, ref Guid pguidCmdGroup, int nCmdID) => NativeMethods.S_FALSE;
                public int GetOptionKeyPath(out string pchKey, int dw) { pchKey = null; return NativeMethods.E_NOTIMPL; }
                public int GetDropTarget(IntPtr pDropTarget, out IntPtr ppDropTarget) { ppDropTarget = IntPtr.Zero; return NativeMethods.E_NOTIMPL; }
                public int GetExternal(out object ppDispatch) { ppDispatch = null; return NativeMethods.E_NOTIMPL; }
                public int ShowUI(int dwID, IntPtr pActiveObject, IntPtr pCommandTarget, IntPtr pFrame, IntPtr pDoc) => NativeMethods.S_FALSE;
                public int HideUI() => NativeMethods.S_OK;
                public int UpdateUI() => NativeMethods.S_OK;
                public int EnableModeless(bool fEnable) => NativeMethods.S_OK;
                public int OnDocWindowActivate(bool fActivate) => NativeMethods.S_OK;
                public int OnFrameWindowActivate(bool fActivate) => NativeMethods.S_OK;
                public int ResizeBorder(IntPtr prcBorder, IntPtr pUIWindow, bool fFrameWindow) => NativeMethods.S_OK;
            }
        }

    [SuppressUnmanagedCodeSecurity]
    internal static class NativeMethods
    {
        public const int S_OK = 0;
        public const int S_FALSE = 1;
        public const int E_NOTIMPL = unchecked((int)0x80004001);

        [StructLayout(LayoutKind.Sequential)]
        public struct DOCHOSTUIINFO
        {
            public int cbSize;
            public DOCHOSTUIFLAG dwFlags;
            public DOCHOSTUIDBLCLK dwDoubleClick;
            public IntPtr pchHostCss;
            public IntPtr pchHostNS;
        }

        [Flags]
        public enum DOCHOSTUIFLAG : int
        {
            // There are many flags; we only need this one
            DPI_AWARE = unchecked((int)0x40000000),
        }

        public enum DOCHOSTUIDBLCLK : int
        {
            DEFAULT = 0,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point pt;
        }

        [ComImport, Guid("BD3F23C0-D43E-11CF-893B-00AA00BDCE1A"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IDocHostUIHandler
        {
            [PreserveSig] int ShowContextMenu(int dwID, IntPtr pt, IntPtr pcmdtReserved, IntPtr pdispReserved);
            [PreserveSig] int GetHostInfo(ref DOCHOSTUIINFO info);
            [PreserveSig] int ShowUI(int dwID, IntPtr pActiveObject, IntPtr pCommandTarget, IntPtr pFrame, IntPtr pDoc);
            [PreserveSig] int HideUI();
            [PreserveSig] int UpdateUI();
            [PreserveSig] int EnableModeless(bool fEnable);
            [PreserveSig] int OnDocWindowActivate(bool fActivate);
            [PreserveSig] int OnFrameWindowActivate(bool fActivate);
            [PreserveSig] int ResizeBorder(IntPtr prcBorder, IntPtr pUIWindow, bool fFrameWindow);
            [PreserveSig] int TranslateAccelerator(ref MSG lpmsg, ref Guid pguidCmdGroup, int nCmdID);
            [PreserveSig] int GetOptionKeyPath(out string pchKey, int dw);
            [PreserveSig] int GetDropTarget(IntPtr pDropTarget, out IntPtr ppDropTarget);
            [PreserveSig] int GetExternal(out object ppDispatch);
            [PreserveSig] int TranslateUrl(int dwTranslate, string strURLIn, out string pstrURLOut);
            [PreserveSig] int FilterDataObject(IntPtr pDO, out IntPtr ppDORet);
        }
    }
}
