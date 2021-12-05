using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

namespace MainScreenCapture4WPF.CaptureAPI
{
    internal class WindowsAPI
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WINRECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern long DwmGetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE dwAttribute, out WINRECT rect, int cbAttribute);

        /// <summary>
        /// ウィンドウ属性
        /// 列挙値の開始は0だとずれていたので1からに変更
        /// </summary>
        enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS, //ウィンドウのRect
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        };

        /// <summary>
        /// 実行されてるすべてのウィンドウのタイトル名取得
        /// </summary>
        /// <returns></returns>
        internal string[] GetAllWindowTitle()
        {
            Process[] ps = Process.GetProcesses();
            return ps.Select(p => p.MainWindowTitle).ToArray();
        }

        /// <summary>
        /// 指定したウィンドウのサイズと位置を取得
        /// </summary>
        /// <param name="title">ウィンドウタイトル</param>
        /// <param name="rect">ウィンドウサイズ・位置</param>
        /// <returns>取得成功: true 取得失敗: false</returns>
        internal bool GetWindowRect(string title, out Rectangle rect)
        {
            // 起動しているすべてのプロセス取得
            Process[] ps = Process.GetProcesses();

            foreach (var p in ps)
            {
                if (title == p.MainWindowTitle)
                {
                    // ウィンドウの位置・サイズ取得
                    DwmGetWindowAttribute(p.MainWindowHandle,
                                          DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                                          out WINRECT winRect,
                                          Marshal.SizeOf(typeof(WINRECT)));

                    // 使いやすいようにRectangleに変換
                    rect = new Rectangle(winRect.left, winRect.top, winRect.right - winRect.left, winRect.bottom - winRect.top);
                    return true;
                }
            }

            rect = new Rectangle();
            return false;
        }
    }
}
