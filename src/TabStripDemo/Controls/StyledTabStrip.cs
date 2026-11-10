using System.ComponentModel;
using System.Drawing.Drawing2D;
using static TabStripDemo.Controls.TabStripTheme;

namespace TabStripDemo.Controls;

/// <summary>
/// GDI+ 오너드로우 탭 스트립.
/// Slanted = 사선 컬러 탭(스타일 1), Rounded = 라운드 탭 + 검색 박스(스타일 2).
/// 탭 헤더만 담당하며, 페이지 전환은 SelectedIndexChanged 에서 처리한다.
/// </summary>
[DefaultEvent(nameof(SelectedIndexChanged))]
public class StyledTabStrip : Control
{
    private const TextFormatFlags TextFlags =
        TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding |
        TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

    private readonly TabItemCollection _tabs;
    private readonly List<RectangleF> _tabBounds = new();
    private readonly TextBox _searchBox;

    private RectangleF _searchToolRect, _addToolRect, _searchBoxRect, _clearRect;
    private TabStripStyle _style = TabStripStyle.Slanted;
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private TabToolButton? _hoverTool;
    private bool _hoverClear;
    private bool _showSearchBox = true;
    private bool _showToolButtons = true;
    private Font? _boldFont;

    public StyledTabStrip()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.Selectable, true);

        _tabs = new TabItemCollection(this);
        _searchBox = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = SubBack,
            ForeColor = SubText,
            PlaceholderText = "검색…",
            Visible = false,
        };
        _searchBox.TextChanged += (_, _) =>
        {
            Invalidate(Rectangle.Ceiling(_searchBoxRect));
            SearchTextChanged?.Invoke(this, EventArgs.Empty);
        };
        Controls.Add(_searchBox);

        // _searchBox 생성 이후에 Font 를 바꿔야 OnFontChanged 에서 안전하다.
        base.BackColor = WindowBack;
        TabStop = true;
        Font = new Font("Segoe UI", 9f);
        UpdateHeight();
    }

    // ------------------------------------------------------------------ API

    public event EventHandler? SelectedIndexChanged;
    public event EventHandler<TabToolButtonEventArgs>? ToolButtonClick;
    public event EventHandler? SearchTextChanged;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TabItemCollection Tabs => _tabs;

    [Category("Appearance"), DefaultValue(TabStripStyle.Slanted)]
    public TabStripStyle Style
    {
        get => _style;
        set
        {
            if (_style == value) return;
            _style = value;
            _hoverIndex = -1;
            _hoverTool = null;
            UpdateHeight();
            LayoutItems();
            Invalidate();
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (value < -1 || value >= _tabs.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (_selectedIndex == value) return;
            _selectedIndex = value;
            Invalidate();
            OnSelectedIndexChanged(EventArgs.Empty);
        }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TabItem? SelectedTab => _selectedIndex >= 0 ? _tabs[_selectedIndex] : null;

    /// <summary>Rounded 스타일에서 오른쪽 검색 박스 표시.</summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool ShowSearchBox
    {
        get => _showSearchBox;
        set { _showSearchBox = value; LayoutItems(); Invalidate(); }
    }

    /// <summary>Slanted 스타일에서 오른쪽 검색/추가 툴버튼 표시.</summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool ShowToolButtons
    {
        get => _showToolButtons;
        set { _showToolButtons = value; LayoutItems(); Invalidate(); }
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string SearchText
    {
        get => _searchBox.Text;
        set => _searchBox.Text = value;
    }

    [Category("Appearance"), DefaultValue("검색…")]
    public string SearchPlaceholder
    {
        get => _searchBox.PlaceholderText;
        set => _searchBox.PlaceholderText = value;
    }

    /// <summary>검색 박스 TextBox 에 포커스. Slanted 의 검색 툴버튼 처리 등에 사용.</summary>
    public void FocusSearchBox()
    {
        if (_searchBox.Visible) _searchBox.Focus();
    }

    /// <summary>점(클라이언트 좌표) 아래의 탭 인덱스. 없으면 -1.</summary>
    public int HitTest(Point p)
    {
        if (_style == TabStripStyle.Slanted)
        {
            if (p.X >= TabsClipRight) return -1;
            // 선택 탭이 맨 위, 그 다음은 왼쪽 탭이 오른쪽 탭 위에 겹쳐 있다.
            if (_selectedIndex >= 0 && SlantedHit(_selectedIndex, p)) return _selectedIndex;
            for (int i = 0; i < _tabs.Count; i++)
                if (i != _selectedIndex && SlantedHit(i, p)) return i;
            return -1;
        }

        if (p.X >= TabsClipRight) return -1;
        for (int i = 0; i < _tabs.Count; i++)
            if (GetRoundedTabRect(i).Contains(p)) return i;
        return -1;
    }

    protected virtual void OnSelectedIndexChanged(EventArgs e) => SelectedIndexChanged?.Invoke(this, e);

    internal void OnTabsChanged()
    {
        LayoutItems();
        if (_tabs.Count == 0)
        {
            if (_selectedIndex != -1) { _selectedIndex = -1; OnSelectedIndexChanged(EventArgs.Empty); }
        }
        else if (_selectedIndex < 0 || _selectedIndex >= _tabs.Count)
        {
            SelectedIndex = Math.Clamp(_selectedIndex, 0, _tabs.Count - 1);
        }
        Invalidate();
    }

    // --------------------------------------------------------------- layout

    protected override Size DefaultSize => new(600, SlantedHeight);

    private float S(float v) => v * DeviceDpi / 96f;

    private Font BoldFont => _boldFont ??= new Font(Font, FontStyle.Bold);

    private float TabsClipRight =>
        _style == TabStripStyle.Slanted
            ? (_showToolButtons ? _searchToolRect.Left - S(4) : Width)
            : (_showSearchBox ? _searchBoxRect.Left - S(8) : Width);

    private void UpdateHeight()
    {
        Height = (int)Math.Ceiling(S(_style == TabStripStyle.Slanted ? SlantedHeight : RoundedHeight));
    }

    private float MeasureTabText(string text) =>
        TextRenderer.MeasureText(text, BoldFont, Size.Empty, TextFlags).Width;

    private static string DisplayText(TabItem tab, TabStripStyle style) =>
        style == TabStripStyle.Rounded ? tab.Text.ToUpperInvariant() : tab.Text;

    private void LayoutItems()
    {
        // 베이스 Control 생성자에서 크기 변경 이벤트가 먼저 올 수 있다.
        if (_tabs is null || _searchBox is null) return;
        _tabBounds.Clear();
        float h = Height;

        if (_style == TabStripStyle.Slanted)
        {
            float x = S(4);
            foreach (var tab in _tabs)
            {
                float w = MeasureTabText(tab.Text) + S(SlantedPadLeft) + S(SlantedPadRight);
                _tabBounds.Add(new RectangleF(x, 0, w, h));
                x += w - S(SlantedOverlap);
            }

            float tw = S(ToolButtonWidth), th = S(ToolButtonHeight);
            _addToolRect = new RectangleF(Width - tw - S(2), h - th, tw, th);
            _searchToolRect = new RectangleF(_addToolRect.Left - S(ToolButtonGap) - tw, h - th, tw, th);
            _searchBox.Visible = false;
        }
        else
        {
            float x = S(RoundedStartX);
            foreach (var tab in _tabs)
            {
                float w = MeasureTabText(DisplayText(tab, _style)) + S(RoundedPadX) * 2;
                _tabBounds.Add(new RectangleF(x, 0, w, h));
                x += w + S(RoundedGap);
            }

            float sw = S(SearchBoxWidth);
            _searchBoxRect = new RectangleF(Width - sw - 1, 0, sw, h);
            float cs = S(18);
            _clearRect = new RectangleF(_searchBoxRect.Right - S(8) - cs, (h - cs) / 2f, cs, cs);

            int tbX = (int)(_searchBoxRect.Left + S(32));
            int tbH = _searchBox.PreferredHeight;
            _searchBox.SetBounds(tbX, (int)((h - tbH) / 2f) + 1, Math.Max(10, (int)(_clearRect.Left - S(6)) - tbX), tbH);
            _searchBox.Visible = _showSearchBox;
        }
    }

    private RectangleF GetSlantedTabRect(int i)
    {
        var b = _tabBounds[i];
        float th = i == _selectedIndex ? Height : S(i == _hoverIndex ? SlantedHoverHeight : SlantedTabHeight);
        return new RectangleF(b.X, Height - th, b.Width, th);
    }

    private RectangleF GetRoundedTabRect(int i)
    {
        var b = _tabBounds[i];
        float th = S(i == _selectedIndex ? RoundedSelHeight : RoundedTabHeight);
        return new RectangleF(b.X, Height - th, b.Width, th);
    }

    private bool SlantedHit(int i, Point p)
    {
        using var path = SlantedTab(GetSlantedTabRect(i), S(SlantedRadius), S(SlantedSlant));
        return path.IsVisible(p);
    }

    private TabToolButton? HitTool(Point p)
    {
        if (_style != TabStripStyle.Slanted || !_showToolButtons) return null;
        if (_searchToolRect.Contains(p)) return TabToolButton.Search;
        if (_addToolRect.Contains(p)) return TabToolButton.Add;
        return null;
    }

    private bool HitClear(Point p) =>
        _style == TabStripStyle.Rounded && _showSearchBox && _searchBox.TextLength > 0 && _clearRect.Contains(p);

    // ---------------------------------------------------------------- paint

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        if (_style == TabStripStyle.Slanted) PaintSlanted(g);
        else PaintRounded(g);
    }

    private void PaintSlanted(Graphics g)
    {
        var state = g.Save();
        g.SetClip(new RectangleF(0, 0, TabsClipRight, Height));

        // 오른쪽 탭부터 그려야 왼쪽 탭의 사선이 위에 겹친다. 선택 탭은 마지막.
        for (int i = _tabs.Count - 1; i >= 0; i--)
            if (i != _selectedIndex) DrawSlantedTab(g, i);
        if (_selectedIndex >= 0) DrawSlantedTab(g, _selectedIndex);

        g.Restore(state);

        if (_showToolButtons)
        {
            DrawToolButton(g, _searchToolRect, TabToolButton.Search);
            DrawToolButton(g, _addToolRect, TabToolButton.Add);
        }
    }

    private void DrawSlantedTab(Graphics g, int i)
    {
        var tab = _tabs[i];
        bool selected = i == _selectedIndex;
        var r = GetSlantedTabRect(i);
        if (r.Width <= 0 || r.Height <= 0) return;

        Color top, bottom, fore;
        if (tab.AccentColor.IsEmpty)
        {
            (top, bottom, fore) = selected
                ? (SlateTop, SlateBottom, Color.White)
                : (GrayTop, GrayBottom, GrayText);
        }
        else
        {
            top = Lighten(tab.AccentColor, 0.2f);
            bottom = tab.AccentColor;
            fore = Color.White;
        }

        using (var path = SlantedTab(r, S(SlantedRadius), S(SlantedSlant)))
        using (var brush = new LinearGradientBrush(RectangleF.Inflate(r, 0, 1), top, bottom, LinearGradientMode.Vertical))
        {
            g.FillPath(brush, path);
        }

        // 겹친 탭 경계가 보이도록 사선에 옅은 그림자 선.
        using (var pen = new Pen(Color.FromArgb(45, 0, 0, 0), 1f))
            g.DrawLine(pen, r.Right - S(SlantedSlant), r.Top + 1, r.Right, r.Bottom);

        var textRect = Rectangle.Round(new RectangleF(
            r.X + S(SlantedPadLeft), r.Y, r.Width - S(SlantedPadLeft) - S(SlantedPadRight), r.Height));
        TextRenderer.DrawText(g, tab.Text, selected ? BoldFont : Font, textRect, fore, TextFlags | TextFormatFlags.Left);

        if (selected && Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(textRect, 4, -5), fore, bottom);
    }

    private void DrawToolButton(Graphics g, RectangleF rect, TabToolButton kind)
    {
        bool hot = _hoverTool == kind;
        var (top, bottom, fore) = kind == TabToolButton.Add
            ? (BlueTop, BlueBottom, Color.White)
            : (GrayTop, GrayBottom, GrayText);
        if (hot)
        {
            top = Lighten(top, 0.15f);
            bottom = Lighten(bottom, 0.1f);
        }

        using (var path = RoundedTop(rect, S(SlantedRadius)))
        using (var brush = new LinearGradientBrush(RectangleF.Inflate(rect, 0, 1), top, bottom, LinearGradientMode.Vertical))
        {
            g.FillPath(brush, path);
        }

        float cx = rect.X + rect.Width / 2f, cy = rect.Y + rect.Height / 2f + S(1);
        using var pen = new Pen(fore, S(1.8f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (kind == TabToolButton.Search)
            DrawSearchGlyph(g, pen, cx, cy);
        else
        {
            float a = S(5);
            g.DrawLine(pen, cx - a, cy, cx + a, cy);
            g.DrawLine(pen, cx, cy - a, cx, cy + a);
        }
    }

    private void DrawSearchGlyph(Graphics g, Pen pen, float cx, float cy)
    {
        float rr = S(4), o = S(1.5f);
        g.DrawEllipse(pen, cx - rr - o, cy - rr - o, rr * 2, rr * 2);
        g.DrawLine(pen, cx + S(1.4f), cy + S(1.4f), cx + S(4.5f), cy + S(4.5f));
    }

    private void PaintRounded(Graphics g)
    {
        var state = g.Save();
        g.SetClip(new RectangleF(0, 0, TabsClipRight, Height));
        for (int i = 0; i < _tabs.Count; i++) DrawRoundedTab(g, i);
        g.Restore(state);

        if (_showSearchBox) DrawSearchBox(g);
    }

    private void DrawRoundedTab(Graphics g, int i)
    {
        var tab = _tabs[i];
        bool selected = i == _selectedIndex, hot = i == _hoverIndex;
        var r = GetRoundedTabRect(i);
        var outline = new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height);

        var (top, bottom, border, fore) = selected
            ? (BlueTop, BlueBottom, BlueBottom, Color.White)
            : (SubBack, GrayTop, SubSeparator, hot ? BlueBottom : GrayText);

        using (var path = RoundedTop(r, S(RoundedRadius)))
        using (var brush = new LinearGradientBrush(RectangleF.Inflate(r, 0, 1), top, bottom, LinearGradientMode.Vertical))
        {
            g.FillPath(brush, path);
        }
        using (var path = RoundedTop(outline, S(RoundedRadius), closed: false))
        using (var pen = new Pen(border, 1f))
        {
            g.DrawPath(pen, path);
        }

        var textRect = Rectangle.Round(r);
        TextRenderer.DrawText(g, DisplayText(tab, _style), selected ? BoldFont : Font, textRect, fore,
            TextFlags | TextFormatFlags.HorizontalCenter);

        if (selected && Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(g, Rectangle.Inflate(textRect, -5, -5), fore, bottom);
    }

    private void DrawSearchBox(Graphics g)
    {
        var r = _searchBoxRect;
        using (var path = RoundedTop(r, S(SearchBoxRadius)))
        using (var brush = new SolidBrush(SubBack))
        {
            g.FillPath(brush, path);
        }
        using (var path = RoundedTop(new RectangleF(r.X + 0.5f, r.Y + 0.5f, r.Width - 1, r.Height), S(SearchBoxRadius), closed: false))
        using (var pen = new Pen(SubSeparator, 1f))
        {
            g.DrawPath(pen, path);
        }

        using (var pen = new Pen(SubCaption, S(1.6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            DrawSearchGlyph(g, pen, r.X + S(16), r.Y + r.Height / 2f);

        if (_searchBox.TextLength > 0)
        {
            using (var brush = new SolidBrush(_hoverClear ? SubText : SubCaption))
                g.FillEllipse(brush, _clearRect);
            float cx = _clearRect.X + _clearRect.Width / 2f, cy = _clearRect.Y + _clearRect.Height / 2f, a = S(3.2f);
            using var pen = new Pen(SubBack, S(1.6f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, cx - a, cy - a, cx + a, cy + a);
            g.DrawLine(pen, cx + a, cy - a, cx - a, cy + a);
        }
    }

    // ---------------------------------------------------------------- input

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int hit = HitTest(e.Location);
        var tool = HitTool(e.Location);
        bool clear = HitClear(e.Location);

        if (hit != _hoverIndex || tool != _hoverTool || clear != _hoverClear)
        {
            _hoverIndex = hit;
            _hoverTool = tool;
            _hoverClear = clear;
            Invalidate();
        }
        Cursor = hit >= 0 || tool != null || clear ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverIndex != -1 || _hoverTool != null || _hoverClear)
        {
            _hoverIndex = -1;
            _hoverTool = null;
            _hoverClear = false;
            Invalidate();
        }
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        int hit = HitTest(e.Location);
        if (hit >= 0)
        {
            Focus();
            SelectedIndex = hit;
            return;
        }

        var tool = HitTool(e.Location);
        if (tool != null)
        {
            ToolButtonClick?.Invoke(this, new TabToolButtonEventArgs(tool.Value));
            return;
        }

        if (HitClear(e.Location))
        {
            _searchBox.Clear();
            _searchBox.Focus();
        }
    }

    protected override bool IsInputKey(Keys keyData) =>
        keyData is Keys.Left or Keys.Right or Keys.Home or Keys.End || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_tabs.Count == 0) return;
        int next = e.KeyCode switch
        {
            Keys.Left => Math.Max(0, _selectedIndex - 1),
            Keys.Right => Math.Min(_tabs.Count - 1, _selectedIndex + 1),
            Keys.Home => 0,
            Keys.End => _tabs.Count - 1,
            _ => _selectedIndex,
        };
        if (next != _selectedIndex)
        {
            SelectedIndex = next;
            e.Handled = true;
        }
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    // ------------------------------------------------------------ lifecycle

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        LayoutItems();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        _boldFont?.Dispose();
        _boldFont = null;
        if (_searchBox is not null) _searchBox.Font = Font;
        base.OnFontChanged(e);
        LayoutItems();
        Invalidate();
    }

    // 높이는 DeviceDpi 로 직접 계산하므로 폼 자동 스케일에서 높이는 제외.
    protected override void ScaleControl(SizeF factor, BoundsSpecified specified) =>
        base.ScaleControl(factor, specified & ~BoundsSpecified.Height);

    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateHeight();
        LayoutItems();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _boldFont?.Dispose();
            _boldFont = null;
        }
        base.Dispose(disposing);
    }
}
