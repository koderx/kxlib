using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace KxLib.UI {
    public static class WindowHelper {
        /// <summary>
        /// 使窗体不可触碰
        /// </summary>
        /// <param name="wpfWindow">WPF窗体</param>
        public static void TransparentWindow(Window wpfWindow) {
            if (wpfWindow == null)
                return;

            wpfWindow.SourceInitialized += delegate {
                IntPtr hwnd = (new WindowInteropHelper(wpfWindow)).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            };
        }
        private static int WindowMargin = 0;
        private static int MinWidth = 0;
        private static int MinHeight = 0;
        /// <summary>
        /// 修复最大化覆盖任务栏的问题
        /// </summary>
        /// <param name="wpfWindow">WPF窗体</param>
        /// <param name="margin">窗体边界</param>
        /// <param name="minWidth">最小宽度</param>
        /// <param name="minHeight">最小高度</param>
        public static void RepairWindowBehavior(Window wpfWindow, int margin = 0,int minWidth=8,int minHeight=8) {
            if (wpfWindow == null)
                return;
            WindowMargin = margin;
            MinWidth = minWidth;
            MinHeight = minHeight;

            wpfWindow.SourceInitialized += delegate {
                IntPtr handle = (new WindowInteropHelper(wpfWindow)).Handle;
                HwndSource source = HwndSource.FromHwnd(handle);
                if (source != null) {
                    source.AddHook(WindowProc);
                }
            };
        }

        private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            switch (msg) {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (IntPtr)0;
        }

        private static void WmGetMinMaxInfo(IntPtr hwnd, IntPtr lParam) {
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero) {
                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left) - WindowMargin;
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top) - WindowMargin;
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left) + WindowMargin * 2;
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top) + WindowMargin * 2;
                mmi.ptMinTrackSize.x = MinWidth;
                mmi.ptMinTrackSize.y = MinHeight;
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);

        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);

        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);

        #region Nested type: MINMAXINFO
        [StructLayout(LayoutKind.Sequential)]
        internal struct MINMAXINFO {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }
        #endregion

        #region Nested type: MONITORINFO
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MONITORINFO {
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }
        #endregion

        #region Nested type: POINT
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT {
            public int x;
            public int y;
            public POINT(int x, int y) {
                this.x = x;
                this.y = y;
            }
        }
        #endregion

        #region Nested type: RECT
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        internal struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public static readonly RECT Empty;

            public int Width {
                get { return Math.Abs(right - left); }
            }
            public int Height {
                get { return bottom - top; }
            }

            public RECT(int left, int top, int right, int bottom) {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public RECT(RECT rcSrc) {
                left = rcSrc.left;
                top = rcSrc.top;
                right = rcSrc.right;
                bottom = rcSrc.bottom;
            }

            public bool IsEmpty {
                get {
                    return left >= right || top >= bottom;
                }
            }

            public override string ToString() {
                if (this == Empty) {
                    return "RECT {Empty}";
                }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            public override bool Equals(object obj) {
                if (!(obj is Rect)) {
                    return false;
                }
                return (this == (RECT)obj);
            }

            public override int GetHashCode() {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }

            public static bool operator ==(RECT rect1, RECT rect2) {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            public static bool operator !=(RECT rect1, RECT rect2) {
                return !(rect1 == rect2);
            }
        }
        #endregion
    }
}