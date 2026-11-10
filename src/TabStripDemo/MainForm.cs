using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;
using TabStripDemo.Controls;
using static TabStripDemo.NativeMethods;

namespace TabStripDemo
{
    /// <summary>
    /// 프레임리스 XtraForm.
    ///  - 타이틀바/가장자리 히트 테스트는 폼 WndProc(WM_NCHITTEST)에서 처리 → 드래그, 더블클릭 최대화, Aero Snap, 리사이즈 모두 네이티브 동작
    ///  - StyledTabStrip(헤더) + SubMenuBar(하위 메뉴) + XtraTabControl(헤더 숨김, 페이지 전환 전용)
    /// </summary>
    public sealed class MainForm : XtraForm
    {
        private const int ResizeBorder = 6;
        private const int TitleBarHeight = 34;

        private readonly HitTransparentPanel _titleBar;
        private readonly HitTransparentPanel _body;
        private readonly Button _maxButton;
        private readonly StyledTabStrip _tabStrip;
        private readonly SubMenuBar _subBar;
        private readonly LabelControl _breadcrumb;
        private readonly XtraTabControl _pages;
        private readonly List<GridView> _views = new List<GridView>();

        public MainForm()
        {
            SuspendLayout();
            Text = "BoardLink";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1100, 720);
            MinimumSize = new Size(760, 480);
            BackColor = TabStripTheme.WindowBack;

            // ---------- 타이틀바 ----------
            _titleBar = new HitTransparentPanel
            {
                Dock = DockStyle.Top,
                Height = TitleBarHeight,
                BackColor = TabStripTheme.Chrome,
                PassThrough = _ => true, // 버튼(자식 창)이 아닌 영역은 모두 폼이 HTCAPTION 처리
            };
            _titleBar.Paint += PaintTitleBar;

            var styleButton = CreateCaptionButton("스타일 전환", isGlyph: false);
            styleButton.Width = 96;
            styleButton.Click += (s, e) => ToggleStyle();
            var minButton = CreateCaptionButton("");
            minButton.Click += (s, e) => WindowState = FormWindowState.Minimized;
            _maxButton = CreateCaptionButton("");
            _maxButton.Click += (s, e) => ToggleMaximize();
            var closeButton = CreateCaptionButton("");
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(0xD4, 0x38, 0x2C);
            closeButton.Click += (s, e) => Close();
            // Dock=Right 는 나중에 추가한 컨트롤이 가장 오른쪽.
            _titleBar.Controls.AddRange(new Control[] { styleButton, minButton, _maxButton, closeButton });

            // ---------- 본문 ----------
            _body = new HitTransparentPanel
            {
                Dock = DockStyle.Fill,
                BackColor = TabStripTheme.WindowBack,
                Padding = new Padding(16, 14, 16, 16),
                PassThrough = screen => IsNearEdge(PointToClient(screen)),
            };

            _tabStrip = new StyledTabStrip { Dock = DockStyle.Top, Style = TabStripStyle.Slanted, SearchPlaceholder = "보드, 빌드 번호 검색…" };
            _subBar = new SubMenuBar { Dock = DockStyle.Top };
            _breadcrumb = new LabelControl
            {
                Dock = DockStyle.Top,
                AutoSizeMode = LabelAutoSizeMode.None,
                Height = 34,
                Padding = new Padding(2, 12, 0, 6),
            };
            _breadcrumb.Appearance.ForeColor = TabStripTheme.SubCaption;

            _pages = new XtraTabControl { Dock = DockStyle.Fill, ShowTabHeader = DefaultBoolean.False };

            var data = SampleData.Create().ToList();
            foreach (var (tab, table) in data)
            {
                var page = new XtraTabPage { Text = tab.Text };
                var grid = new GridControl { Dock = DockStyle.Fill };
                var view = new GridView(grid);
                grid.MainView = view;
                grid.DataSource = table;
                view.OptionsView.ShowGroupPanel = false;
                view.OptionsView.ShowIndicator = false;
                view.OptionsBehavior.Editable = false;
                view.OptionsFind.AlwaysVisible = false;
                page.Controls.Add(grid);
                _pages.TabPages.Add(page);
                _views.Add(view);
                tab.Tag = page;
            }

            // Dock 처리 순서: 컬렉션 뒤쪽부터 → Fill 을 먼저 추가하고 Top 은 아래에서 위 순서로 추가.
            _body.Controls.Add(_pages);
            _body.Controls.Add(_breadcrumb);
            _body.Controls.Add(_subBar);
            _body.Controls.Add(_tabStrip);

            Controls.Add(_body);
            Controls.Add(_titleBar);

            // ---------- 이벤트 ----------
            _tabStrip.SelectedIndexChanged += (s, e) => OnTabChanged();
            _tabStrip.ToolButtonClick += OnToolButtonClick;
            _tabStrip.SearchTextChanged += (s, e) => ApplySearch();
            _subBar.ItemClick += (s, e) => UpdateBreadcrumb();

            foreach (var item in data)
                _tabStrip.Tabs.Add(item.Tab); // 첫 탭 추가 시 SelectedIndex=0 → OnTabChanged

            // 위 픽셀 값은 96 DPI 기준. 커스텀 컨트롤은 자체적으로 DeviceDpi 로 높이를 계산한다.
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ResumeLayout(false);
        }

        // ------------------------------------------------------------ 탭 연동

        private void OnTabChanged()
        {
            var tab = _tabStrip.SelectedTab;
            if (tab == null) return;
            _subBar.AccentColor = tab.AccentColor;
            _subBar.SetItems(tab.SubItems);
            _pages.SelectedTabPageIndex = _tabStrip.SelectedIndex;
            ApplySearch();
            UpdateBreadcrumb();
        }

        private void UpdateBreadcrumb()
        {
            var tab = _tabStrip.SelectedTab;
            var sub = _subBar.SelectedItem;
            _breadcrumb.Text = tab == null ? string.Empty : sub == null ? tab.Text : $"{tab.Text}  ›  {sub.Title}";
        }

        private void ApplySearch()
        {
            int i = _tabStrip.SelectedIndex;
            if (i >= 0 && i < _views.Count)
                _views[i].ApplyFindFilter(_tabStrip.SearchText);
        }

        private void OnToolButtonClick(object sender, TabToolButtonEventArgs e)
        {
            if (e.Button == TabToolButton.Search)
            {
                // Slanted 스타일은 검색 박스가 없으므로 그리드 찾기 패널을 연다.
                int i = _tabStrip.SelectedIndex;
                if (i >= 0 && i < _views.Count) _views[i].ShowFindPanel();
            }
            else
            {
                XtraMessageBox.Show(this, "새 탭 추가 동작을 여기에 연결하세요.", Text);
            }
        }

        private void ToggleStyle()
        {
            var style = _tabStrip.Style == TabStripStyle.Slanted ? TabStripStyle.Rounded : TabStripStyle.Slanted;
            _tabStrip.Style = style;
            _subBar.Style = style;
            if (style == TabStripStyle.Slanted) _tabStrip.SearchText = string.Empty;
        }

        // ------------------------------------------------------- 프레임리스 처리

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX | WS_MAXIMIZEBOX; // 작업표시줄 최소화, 캡션 더블클릭 최대화
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == HTCLIENT)
                    m.Result = (IntPtr)HitTestFrame(PointToClient(PointFromLParam(m.LParam)));
                return;
            }
            base.WndProc(ref m);
        }

        private int Px(int v) => (int)Math.Round(v * DeviceDpi / 96f);

        private bool IsNearEdge(Point p)
        {
            if (WindowState == FormWindowState.Maximized) return false;
            int b = Px(ResizeBorder);
            return p.X < b || p.Y < b || p.X >= ClientSize.Width - b || p.Y >= ClientSize.Height - b;
        }

        private int HitTestFrame(Point p)
        {
            if (WindowState != FormWindowState.Maximized)
            {
                int b = Px(ResizeBorder);
                bool left = p.X < b, right = p.X >= ClientSize.Width - b;
                bool top = p.Y < b, bottom = p.Y >= ClientSize.Height - b;
                if (top && left) return HTTOPLEFT;
                if (top && right) return HTTOPRIGHT;
                if (bottom && left) return HTBOTTOMLEFT;
                if (bottom && right) return HTBOTTOMRIGHT;
                if (left) return HTLEFT;
                if (right) return HTRIGHT;
                if (top) return HTTOP;
                if (bottom) return HTBOTTOM;
            }
            return p.Y < _titleBar.Bottom ? HTCAPTION : HTCLIENT;
        }

        private void ToggleMaximize()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        /// <summary>테두리 없는 창이 최대화될 때 작업표시줄을 덮지 않도록 현재 모니터 작업 영역으로 제한.</summary>
        private void UpdateMaximizedBounds()
        {
            if (!IsHandleCreated) return;
            var screen = Screen.FromHandle(Handle);
            var wa = screen.WorkingArea;
            MaximizedBounds = new Rectangle(wa.X - screen.Bounds.X, wa.Y - screen.Bounds.Y, wa.Width, wa.Height);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateMaximizedBounds();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            UpdateMaximizedBounds();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_maxButton != null)
                _maxButton.Text = WindowState == FormWindowState.Maximized ? "" : "";
        }

        // ------------------------------------------------------------ 타이틀바

        private Button CreateCaptionButton(string text, bool isGlyph = true)
        {
            var b = new Button
            {
                Text = text,
                Dock = DockStyle.Right,
                Width = 46,
                FlatStyle = FlatStyle.Flat,
                ForeColor = TabStripTheme.ChromeText,
                BackColor = TabStripTheme.Chrome,
                Font = isGlyph ? new Font("Segoe MDL2 Assets", 7.5f) : new Font("Segoe UI", 8.25f),
                TabStop = false,
                Cursor = Cursors.Default,
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(0x33, 0x3A, 0x44);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(0x40, 0x48, 0x53);
            return b;
        }

        private void PaintTitleBar(object sender, PaintEventArgs e)
        {
            const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
            using (var bold = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var normal = new Font("Segoe UI", 9f))
            {
                int x = Px(12);
                Size nameSize = TextRenderer.MeasureText(e.Graphics, Text, bold, Size.Empty, flags);
                TextRenderer.DrawText(e.Graphics, Text, bold, new Rectangle(x, 0, nameSize.Width, _titleBar.Height), TabStripTheme.ChromeText, flags);
                TextRenderer.DrawText(e.Graphics, "— 장비 관리 콘솔", normal,
                    new Rectangle(x + nameSize.Width + Px(4), 0, Px(240), _titleBar.Height), TabStripTheme.ChromeMuted, flags);
            }
        }
    }
}
