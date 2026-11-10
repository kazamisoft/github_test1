using System;
using System.Drawing;
namespace TabStripDemo
{
    internal static class NativeMethods
    {
        public const int WM_NCHITTEST = 0x0084;

        public const int HTTRANSPARENT = -1;
        public const int HTCLIENT = 1;
        public const int HTCAPTION = 2;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        public const int WS_MAXIMIZEBOX = 0x00010000;
        public const int WS_MINIMIZEBOX = 0x00020000;
        public const int CS_DROPSHADOW = 0x00020000;

        /// <summary>WM_NCHITTEST 의 lParam(스크린 좌표, 부호 있는 16비트 x/y)을 Point 로 변환.</summary>
        public static Point PointFromLParam(IntPtr lParam)
        {
            long v = lParam.ToInt64();
            return new Point(unchecked((short)(v & 0xFFFF)), unchecked((short)((v >> 16) & 0xFFFF)));
        }
    }
}
