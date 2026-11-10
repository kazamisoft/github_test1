using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace TabStripDemo.Controls
{

    /// <summary>탭 스트립/서브 바에서 쓰는 색과 치수(96 DPI 기준) 모음.</summary>
    public static class TabStripTheme
    {
        // ---- 색 ----
        public static readonly Color WindowBack   = Color.FromArgb(0xF7, 0xF8, 0xFA);
        public static readonly Color Chrome       = Color.FromArgb(0x20, 0x26, 0x2E);
        public static readonly Color ChromeText   = Color.FromArgb(0xE9, 0xED, 0xF2);
        public static readonly Color ChromeMuted  = Color.FromArgb(0x8E, 0x98, 0xA5);

        public static readonly Color GrayTop      = Color.FromArgb(0xF6, 0xF7, 0xF9);
        public static readonly Color GrayBottom   = Color.FromArgb(0xE2, 0xE5, 0xE9);
        public static readonly Color GrayText     = Color.FromArgb(0x4D, 0x56, 0x63);
        public static readonly Color SlateTop     = Color.FromArgb(0x6B, 0x76, 0x86);
        public static readonly Color SlateBottom  = Color.FromArgb(0x4A, 0x54, 0x62);
        public static readonly Color BlueTop      = Color.FromArgb(0x62, 0xAE, 0xE9);
        public static readonly Color BlueBottom   = Color.FromArgb(0x2F, 0x7F, 0xC8);

        public static readonly Color SubBack      = Color.White;
        public static readonly Color SubText      = Color.FromArgb(0x2B, 0x32, 0x3B);
        public static readonly Color SubCaption   = Color.FromArgb(0x8A, 0x93, 0xA0);
        public static readonly Color SubSeparator = Color.FromArgb(0xE6, 0xE9, 0xED);
        public static readonly Color SubHover     = GrayTop;

        // ---- 스타일 1 (Slanted) ----
        public const int SlantedHeight      = 34; // 컨트롤 높이 = 선택 탭 높이
        public const int SlantedTabHeight   = 28;
        public const int SlantedHoverHeight = 31;
        public const int SlantedPadLeft     = 16;
        public const int SlantedPadRight    = 26;
        public const int SlantedOverlap     = 12;
        public const int SlantedSlant       = 16;
        public const int SlantedRadius      = 6;
        public const int ToolButtonWidth    = 34;
        public const int ToolButtonHeight   = 26;
        public const int ToolButtonGap      = 4;

        // ---- 스타일 2 (Rounded) ----
        public const int RoundedHeight      = 32;
        public const int RoundedTabHeight   = 28;
        public const int RoundedSelHeight   = 30;
        public const int RoundedPadX        = 18;
        public const int RoundedGap         = 2;
        public const int RoundedRadius      = 6;
        public const int RoundedStartX      = 16;
        public const int SearchBoxWidth     = 250;
        public const int SearchBoxRadius    = 8;

        // ---- 서브 바 ----
        public const int SubBarHeight       = 52; // 띠 포함, 그림자 제외
        public const int SubShadow          = 4;
        public const int SlantedStripe      = 3;
        public const int RoundedStripe      = 2;
        public const int SubIconSize        = 18;
        public const int SubPadX            = 14;

        public static Color Lighten(Color c, float amount)
        {
            amount = Math.Max(0f, Math.Min(1f, amount));
            return Color.FromArgb(c.A,
                (int)(c.R + (255 - c.R) * amount),
                (int)(c.G + (255 - c.G) * amount),
                (int)(c.B + (255 - c.B) * amount));
        }

        /// <summary>스타일 1 탭: 좌상단 라운드 + 오른쪽 변이 아래로 벌어지는 사선(사다리꼴).</summary>
        public static GraphicsPath SlantedTab(RectangleF r, float radius, float slant)
        {
            var p = new GraphicsPath();
            float d = radius * 2;
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddLine(r.Left + radius, r.Top, r.Right - slant, r.Top);
            p.AddLine(r.Right - slant, r.Top, r.Right, r.Bottom);
            p.AddLine(r.Right, r.Bottom, r.Left, r.Bottom);
            p.CloseFigure();
            return p;
        }

        /// <summary>위쪽 두 모서리만 둥근 사각형. closed=false 이면 아래 변 없이 외곽선용 경로.</summary>
        public static GraphicsPath RoundedTop(RectangleF r, float radius, bool closed = true)
        {
            var p = new GraphicsPath();
            float d = radius * 2;
            p.AddLine(r.Left, r.Bottom, r.Left, r.Top + radius);
            p.AddArc(r.Left, r.Top, d, d, 180, 90);
            p.AddLine(r.Left + radius, r.Top, r.Right - radius, r.Top);
            p.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            p.AddLine(r.Right, r.Top + radius, r.Right, r.Bottom);
            if (closed) p.CloseFigure();
            return p;
        }
    }
}
