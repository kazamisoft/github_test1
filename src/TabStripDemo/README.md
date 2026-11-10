# TabStripDemo — 프레임리스 WinForms 커스텀 탭 스트립

C# WinForms(.NET Framework 4.8) + DevExpress 24.x 프레임리스 창에서 쓰는 오너드로우 탭 스트립 예제입니다.

| 스타일 | 설명 |
|---|---|
| `TabStripStyle.Slanted` (스타일 1) | 탭마다 고유 색, 오른쪽 사선으로 겹친 사다리꼴 탭, 선택 탭이 서브 바 색 띠와 이어짐, 검색/추가 툴버튼 |
| `TabStripStyle.Rounded` (스타일 2) | 상단 라운드 탭, 선택 탭만 파란 그라데이션, 오른쪽 검색 박스(내장 TextBox + 지우기 버튼) |

실행 중 타이틀바의 **스타일 전환** 버튼으로 두 스타일을 바꿔 볼 수 있습니다.

## 구조

```
MainForm : XtraForm (FormBorderStyle.None)
├─ HitTransparentPanel (타이틀바)   WM_NCHITTEST → 폼이 HTCAPTION/리사이즈 처리
├─ HitTransparentPanel (본문)
│   ├─ StyledTabStrip              Controls/StyledTabStrip.cs
│   ├─ SubMenuBar                  Controls/SubMenuBar.cs
│   ├─ LabelControl (경로 표시)
│   └─ XtraTabControl              ShowTabHeader = False, 탭당 GridControl 한 페이지
```

- `Controls/` 의 두 컨트롤은 DevExpress에 의존하지 않는 순수 GDI+ 컨트롤입니다. 다른 프로젝트에 그대로 복사해 쓸 수 있습니다.
- 색·치수는 `Controls/TabStripTheme.cs` 상수(96 DPI 기준)로 모여 있고, 그릴 때 `DeviceDpi` 로 스케일합니다.
- 프레임리스 처리: 타이틀바와 가장자리 6px 은 자식 패널이 `HTTRANSPARENT` 를 돌려주고 폼이 `HTCAPTION`/`HTLEFT`… 를 돌려줍니다. 그래서 드래그, 더블클릭 최대화, Aero Snap, 가장자리 리사이즈가 모두 OS 기본 동작으로 됩니다. 최대화 시 작업표시줄을 덮지 않도록 `MaximizedBounds` 를 모니터 작업 영역으로 맞춥니다.

## 사용 예

```csharp
var strip = new StyledTabStrip { Dock = DockStyle.Top, Style = TabStripStyle.Slanted };
var sub   = new SubMenuBar     { Dock = DockStyle.Top };

var dev = strip.Tabs.Add("디바이스", Color.FromArgb(0xD9, 0x41, 0x2B));
dev.SubItems.Add(new SubItem("보드 목록", "AM62x · i.MX8M · CV25"));

strip.SelectedIndexChanged += (_, _) =>
{
    sub.AccentColor = strip.SelectedTab!.AccentColor;
    sub.SetItems(strip.SelectedTab.SubItems);
    xtraTabControl1.SelectedTabPageIndex = strip.SelectedIndex;
};
```

## 빌드 환경

- **.NET Framework 4.8, C# 7.3**, DevExpress WinForms 24.x
- `Controls/` 폴더는 C# 7.3 문법만 쓰므로 기존 .NET Framework 프로젝트(구형 csproj 포함)에 그대로 복사해 쓸 수 있습니다. 필요한 최소 버전은 .NET Framework 4.7입니다(`DeviceDpi`, `OnDpiChangedAfterParent`).
- 플레이스홀더는 .NET Framework에 `TextBox.PlaceholderText`가 없어서 `CueTextBox`(EM_SETCUEBANNER)로 구현했습니다. `Application.EnableVisualStyles()`가 호출되어 있어야 보입니다.
- 고DPI: `App.config`의 `DpiAwareness=PerMonitorV2`와 `app.manifest`의 Windows 10 호환 선언이 함께 있어야 합니다. 기존 프로젝트에도 두 설정을 옮겨 주세요.
- `TabStripDemo.csproj`의 `DevExpress.Win` 버전을 설치된 버전으로 고정하세요. DevExpress 설치 시 생기는 로컬 NuGet 소스를 쓰거나, 구형 csproj라면 DevExpress 어셈블리를 직접 참조해도 됩니다.
- 검증: Linux에서 .NET Framework 4.8 참조 어셈블리 + `LangVersion 7.3` + DevExpress 24.2 평가판 패키지로 전체 컴파일 성공(오류 0, 경고는 라이선스 안내 DX1000/DX1001만). 실제 화면 실행은 Windows에서 확인이 필요합니다.

## 탭 부분만 구현할 때 쓸 프롬프트 (.NET Framework 4.8)

```text
C# WinForms 프로젝트에 오너드로우 탭 헤더 컨트롤을 추가해줘.
탭 헤더만 담당하고, 페이지 전환은 SelectedIndexChanged 이벤트로 바깥에서 처리한다.
DevExpress에 의존하지 않는 순수 GDI+ 컨트롤로 만든다(콘텐츠 영역은 DevExpress 24.x XtraTabControl 사용).

[환경 - 반드시 지킬 것]
- .NET Framework 4.8, C# 7.3 (기존 .NET Framework 프로젝트 기본 언어 버전)
- 다음은 쓰지 말 것: using 선언(using var), switch 식, ??=, 참조형 nullable 표기(string?),
  target-typed new(new()), or/and 패턴, file-scoped namespace, global using, 람다 매개변수 (_, _)
- 다음 API는 .NET Framework에 없으니 쓰지 말 것: Math.Clamp, TextBox.PlaceholderText, ApplicationConfiguration
- 모든 파일에 using을 명시(System, System.Drawing, System.Drawing.Drawing2D, System.Windows.Forms 등)

[파일]
- Controls/StyledTabStrip.cs : 컨트롤 본체
- Controls/TabItem.cs        : TabItem, TabItemCollection, enum, EventArgs
- Controls/TabStripTheme.cs  : 색·치수 상수(96 DPI 기준)와 GraphicsPath 헬퍼
- Controls/CueTextBox.cs     : TextBox 상속. EM_SETCUEBANNER(0x1501, wParam=1)로 안내 문구 표시,
                               OnHandleCreated에서 다시 적용

[공개 API]
- class StyledTabStrip : Control
  - TabItemCollection Tabs    (Collection<TabItem> 상속, 추가·삭제 시 다시 배치하고 다시 그림)
  - TabStripStyle Style       (Slanted | Rounded, 기본 Slanted)
  - int SelectedIndex, TabItem SelectedTab(없으면 null)
  - bool ShowToolButtons      (Slanted 전용, 기본 true)
  - bool ShowSearchBox        (Rounded 전용, 기본 true)
  - string SearchText, string SearchPlaceholder
  - event SelectedIndexChanged, ToolButtonClick(TabToolButtonEventArgs: Search|Add), SearchTextChanged
  - int HitTest(Point)
- class TabItem: string Text, Color AccentColor(Empty면 회색 탭), object Tag
  생성자는 (string)과 (string, Color) 오버로드로 만들 것(Color 기본값 매개변수 대신)
- 런타임 속성에는 [Browsable(false), DesignerSerializationVisibility(Hidden)]

[스타일 1 - Slanted]
- 컨트롤 높이 34. 탭 높이는 비선택 28, 마우스 오버 31, 선택 34이고 모두 아래쪽에 맞춤
- 탭 모양: 왼쪽 위만 반지름 6 라운드, 오른쪽 변은 위에서 아래로 16px 벌어지는 사선(사다리꼴)
- 탭 폭 = 굵은 글꼴로 잰 텍스트 폭 + 왼쪽 16 + 오른쪽 26. 이웃 탭과 12px 겹침, 첫 탭 x=4
- 그리기 순서: 비선택 탭을 오른쪽부터 왼쪽 순서로 그리고, 선택 탭은 마지막에 그림
- 채우기: AccentColor 기준 세로 그라데이션(위쪽은 흰색 쪽으로 20% 밝게)
  회색 탭은 #F6F7F9→#E2E5E9에 글자 #4D5663, 회색 탭이 선택되면 #6B7686→#4A5462에 흰 글자
- 사선 위에 alpha 45 검정 1px 선. 선택 탭 글자는 Bold
- 오른쪽 끝에 툴버튼 2개(34x26, 위쪽 반지름 6, 간격 4)
  검색 버튼은 회색 + 돋보기, 추가 버튼은 #62AEE9→#2F7FC8 + 흰색 '+'
- 툴버튼 왼쪽 4px 지점에서 탭 영역을 잘라냄(SetClip)

[스타일 2 - Rounded]
- 컨트롤 높이 32. 탭 높이는 비선택 28, 선택 30. 위쪽 반지름 6, 탭 간격 2, 첫 탭 x=16
- 탭 텍스트는 ToUpperInvariant, 탭 폭 = 텍스트 폭 + 좌우 18
- 비선택: #FFFFFF→#F6F7F9, 테두리 #E6E9ED(아래 변 없음), 글자 #4D5663, 마우스 오버 시 글자 #2F7FC8
- 선택: #62AEE9→#2F7FC8, 테두리 #2F7FC8, 흰 글자 Bold
- 오른쪽 검색 박스: 폭 250, 높이는 컨트롤 전체, 위쪽 반지름 8, 흰 배경, #E6E9ED 테두리
  - 왼쪽에 돋보기 글리프(#8A93A0)
  - BorderStyle.None인 CueTextBox를 세로 가운데에 배치
  - 텍스트가 있으면 오른쪽에 원형 지우기 버튼(18px, #8A93A0, 마우스 오버 시 #2B323B, 흰 X)
- 검색 박스 왼쪽 8px 지점에서 탭 영역을 잘라냄

[동작]
- SetStyle(AllPaintingInWmPaint | OptimizedDoubleBuffer | ResizeRedraw | UserPaint | Selectable)
- 클릭 판정: Slanted는 GraphicsPath.IsVisible로 하되 선택 탭을 먼저 검사하고, 그다음 왼쪽 탭부터 검사
  Rounded는 사각형 Contains로 판정
- MouseDown에서 탭 선택(+Focus), 툴버튼 클릭 이벤트, 지우기 버튼 처리
- 마우스 오버 상태가 바뀔 때만 Invalidate. 손 모양 커서 사용
- 키보드: IsInputKey에서 Left/Right/Home/End를 받아 선택을 이동.
  포커스가 있고 ShowFocusCues가 true면 선택 탭에 ControlPaint.DrawFocusRectangle
- 탭이 추가되어 SelectedIndex가 -1이면 0으로 바꾸고 이벤트를 발생시킴

[DPI / 품질]
- 모든 치수는 TabStripTheme 상수 × (DeviceDpi / 96)로 계산(DeviceDpi는 4.7+).
  OnDpiChangedAfterParent에서 높이와 배치를 다시 계산
- ScaleControl을 재정의해서 BoundsSpecified.Height를 제외(높이는 직접 계산하므로)
- App.config에 DpiAwareness=PerMonitorV2, app.manifest에 Windows 10 supportedOS 선언 예시도 같이 작성
- Program.Main에서 Application.EnableVisualStyles() 필수(EM_SETCUEBANNER 표시 조건)
- 텍스트는 TextRenderer(NoPadding | NoPrefix | SingleLine | EndEllipsis | VerticalCenter),
  도형은 SmoothingMode.AntiAlias + PixelOffsetMode.HighQuality
- Brush, Pen, GraphicsPath는 using 블록으로 해제. Bold 글꼴은 캐시하고 OnFontChanged와 Dispose에서 해제
- 베이스 Control 생성자에서 OnSizeChanged, OnFontChanged가 먼저 호출될 수 있으므로
  필드를 초기화한 뒤 Font를 설정하고, 배치 함수에 null 가드를 둘 것
- LinearGradientBrush에 폭이나 높이가 0인 사각형을 넘기지 않도록 가드

[사용 예도 같이 작성]
strip.Tabs.Add("디바이스", Color.FromArgb(0xD9, 0x41, 0x2B));
strip.SelectedIndexChanged += (s, e) => xtraTabControl1.SelectedTabPageIndex = strip.SelectedIndex;
(XtraTabControl은 ShowTabHeader = DefaultBoolean.False로 설정)
```
