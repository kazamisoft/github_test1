using System.ComponentModel;
using System.Drawing.Drawing2D;
using static TabStripDemo.Controls.TabStripTheme;

namespace TabStripDemo.Controls;

/// <summary>
/// 탭 아래에 붙는 하위 메뉴 바. 항목마다 아이콘 + 제목(대문자) + 설명 두 줄.
/// 상단 띠 색은 선택된 탭 색(Slanted) 또는 파란색(Rounded).
/// </summary>
[DefaultEvent(nameof(ItemClick))]
public class SubMenuBar : Control
{
    private const TextFormatFlags TextFlags =
        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
        TextFormatFlags.Left | TextFormatFlags.EndEllipsis;

    private readonly List<SubItem> _items = new();
    private TabStripStyle _style = TabStripStyle.Slanted;
    private Color _accentColor = SlateBottom;
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private Font? _titleFont;
    private Font? _captionFont;

    public SubMenuBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        BackColor = WindowBack;
        Font = new Font("Segoe UI", 9f);
        UpdateHeight();
    }

    public event EventHandler<SubItemEventArgs>? ItemClick;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<SubItem> Items => _items;

    [Category("Appearance"), DefaultValue(TabStripStyle.Slanted)]
    public TabStripStyle Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            _style = value;
            Invalidate();
        }
    }

    /// <summary>Slanted 스타일의 상단 띠 및 선택 점 색. 보통 선택된 탭의 AccentColor.</summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value.IsEmpty ? SlateBottom : value; Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            _selectedIndex = value;
            Invalidate();
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SubItem? SelectedItem => _selectedIndex >= 0 ? _items[_selectedIndex] : null;

    public void SetItems(IEnumerable<SubItem> items, int selectedIndex = 0)
    {
        _items.Clear();
        _items.AddRange(items);
        _selectedIndex = _items.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, _items.Count - 1);
        _hoverIndex = -1;
        Invalidate();
    }

    // --------------------------------------------------------------- layout

    protected override Size DefaultSize => new(600, SubBarHeight + SubShadow);

    private float S(float v) => v * DeviceDpi / 96f;

    private Font TitleFont => _titleFont ??= new Font(Font, FontStyle.Bold);
    private Font CaptionFont => _captionFont ??= new Font(Font.FontFamily, Math.Max(6f, Font.Size - 0.75f), FontStyle.Regular);

    private float StripeHeight => S(_style == TabStripStyle.Slanted ? SlantedStripe : RoundedStripe);
    private RectangleF BarRect => new(0, 0, Width, Height - S(SubShadow));

    private void UpdateHeight() => Height = (int)Math.Ceiling(S(SubBarHeight + SubShadow));

    private RectangleF GetItemRect(int i)
    {
        var bar = BarRect;
        float top = StripeHeight;
        float w = bar.Width / Math.Max(1, _items.Count);
        return new RectangleF(bar.X + i * w, top, w, bar.Height - top);
    }

    private int HitTest(Point p)
    {
        for (int i = 0; i < _items.Count; i++)
            if (GetItemRect(i).Contains(p)) return i;
        return -1;
    }

    // ---------------------------------------------------------------- paint

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var bar = BarRect;

        // 아래 그림자
        float sh = S(SubShadow);
        var shadowRect = new RectangleF(bar.X + S(2), bar.Bottom, bar.Width - S(4), sh);
        if (shadowRect.Width > 0 && sh > 0)
        {
            using var brush = new LinearGradientBrush(
                new RectangleF(shadowRect.X, shadowRect.Y - 1, shadowRect.Width, sh + 1),
                Color.FromArgb(45, 0, 0, 0), Color.FromArgb(0, 0, 0, 0), LinearGradientMode.Vertical);
            g.FillRectangle(brush, shadowRect);
        }

        // 본체
        using (var brush = new SolidBrush(SubBack))
            g.FillRectangle(brush, bar);
        if (_style == TabStripStyle.Rounded)
        {
            using var pen = new Pen(SubSeparator);
            g.DrawRectangle(pen, 0, 0, Width - 1, (int)bar.Height - 1);
        }

        // 상단 띠
        using (var brush = new SolidBrush(_style == TabStripStyle.Slanted ? _accentColor : BlueBottom))
            g.FillRectangle(brush, 0, 0, bar.Width, StripeHeight);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        int titleH = TextRenderer.MeasureText("Ag가", TitleFont, Size.Empty, TextFlags).Height;
        int capH = TextRenderer.MeasureText("Ag가", CaptionFont, Size.Empty, TextFlags).Height;

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var r = GetItemRect(i);
            bool selected = i == _selectedIndex, hot = i == _hoverIndex;

            if (hot)
            {
                using var brush = new SolidBrush(SubHover);
                g.FillRectangle(brush, r);
            }

            if (_style == TabStripStyle.Slanted && i > 0)
            {
                g.SmoothingMode = SmoothingMode.None;
                using (var pen = new Pen(SubSeparator))
                    g.DrawLine(pen, (int)r.X, (int)r.Top + S(8), (int)r.X, (int)r.Bottom - S(8));
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }

            float icon = S(SubIconSize);
            var iconRect = new RectangleF(r.X + S(SubPadX), r.Y + (r.Height - icon) / 2f, icon, icon);
            DrawIcon(g, item, iconRect, selected, hot);

            float tx = iconRect.Right + S(10);
            int tw = (int)(r.Right - tx - S(8));
            if (tw <= 0) continue;

            float cy = r.Y + r.Height / 2f;
            var titleRect = new Rectangle((int)tx, (int)(cy - titleH), tw, titleH);
            var capRect = new Rectangle((int)tx, (int)(cy + S(1)), tw, capH);
            TextRenderer.DrawText(g, item.Title.ToUpperInvariant(), TitleFont, titleRect, SubText, TextFlags);
            TextRenderer.DrawText(g, item.Caption, CaptionFont, capRect, SubCaption, TextFlags);
        }
    }

    private void DrawIcon(Graphics g, SubItem item, RectangleF r, bool selected, bool hot)
    {
        if (item.Image != null)
        {
            g.DrawImage(item.Image, r);
            return;
        }

        if (_style == TabStripStyle.Slanted)
        {
            // 원형 버튼 + 가운데 점 (선택 시 탭 색으로 커짐)
            using (var brush = new LinearGradientBrush(RectangleF.Inflate(r, 0, 1), GrayTop, GrayBottom, LinearGradientMode.Vertical))
                g.FillEllipse(brush, r);
            using (var pen = new Pen(SubSeparator))
                g.DrawEllipse(pen, r);

            float dot = S(selected ? 4f : 3f);
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            using var dotBrush = new SolidBrush(selected ? _accentColor : SubCaption);
            g.FillEllipse(dotBrush, cx - dot, cy - dot, dot * 2, dot * 2);
            return;
        }

        // 박스 + 화살표 글리프 (18단위 좌표계)
        float u = r.Width / 18f;
        PointF P(float x, float y) => new(r.X + x * u, r.Y + y * u);
        using var glyphPen = new Pen(selected || hot ? BlueBottom : SubCaption, S(1.6f))
        {
            StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round,
        };
        g.DrawLines(glyphPen, new[] { P(7, 3), P(3.5f, 3), P(3.5f, 15), P(7, 15) });
        g.DrawLine(glyphPen, P(7, 9), P(15, 9));
        g.DrawLines(glyphPen, new[] { P(12, 6), P(15, 9), P(12, 12) });
    }

    // ---------------------------------------------------------------- input

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int hit = HitTest(e.Location);
        if (hit != _hoverIndex)
        {
            _hoverIndex = hit;
            Invalidate();
        }
        Cursor = hit >= 0 ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            Invalidate();
        }
        Cursor = Cursors.Default;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        int hit = HitTest(e.Location);
        if (hit < 0) return;
        SelectedIndex = hit;
        ItemClick?.Invoke(this, new SubItemEventArgs(hit, _items[hit]));
    }

    // ------------------------------------------------------------ lifecycle

    protected override void OnFontChanged(EventArgs e)
    {
        DisposeFonts();
        base.OnFontChanged(e);
        Invalidate();
    }

    // 높이는 DeviceDpi 로 직접 계산하므로 폼 자동 스케일에서 높이는 제외.
    protected override void ScaleControl(SizeF factor, BoundsSpecified specified) =>
        base.ScaleControl(factor, specified & ~BoundsSpecified.Height);

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateHeight();
        Invalidate();
    }

    private void DisposeFonts()
    {
        _titleFont?.Dispose();
        _titleFont = null;
        _captionFont?.Dispose();
        _captionFont = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) DisposeFonts();
        base.Dispose(disposing);
    }
}
