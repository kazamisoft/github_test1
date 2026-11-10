# TabStripDemo — 프레임리스 WinForms 커스텀 탭 스트립

C# WinForms(.NET 8) + DevExpress 프레임리스 창에서 쓰는 오너드로우 탭 스트립 예제입니다.

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

## 빌드

- Visual Studio 2022, .NET 8 SDK, DevExpress WinForms 라이선스/NuGet 피드 필요
- `TabStripDemo.csproj` 의 `DevExpress.Win` 버전을 설치된 버전으로 고정하세요.
- 이 코드는 Windows가 아닌 환경에서 작성되어 **아직 컴파일 검증을 하지 않았습니다.** 첫 빌드에서 나오는 오류는 대부분 DevExpress 버전 차이일 가능성이 큽니다.

## 다른 프로젝트에 붙일 때 쓸 프롬프트

```text
C# WinForms(.NET 8, DevExpress v24.x) 프레임리스 앱에 커스텀 탭 스트립을 구현해줘.

[구성]
1. StyledTabStrip : Control
   - DoubleBuffered, ResizeRedraw, OptimizedDoubleBuffer 켜기
   - 속성: Style(Slanted | Rounded), Tabs(List<TabItem>), SelectedIndex, ShowSearchBox, ShowToolButtons
   - 이벤트: SelectedIndexChanged, SearchTextChanged, ToolButtonClick(Search/Add)
   - TabItem: Text, AccentColor(Color.Empty면 회색 탭), SubItems(List<SubItem>), Tag
   - 탭 폭은 TextRenderer.MeasureText + 좌우 패딩으로 계산, 히트테스트는 GraphicsPath.IsVisible 사용
   - 마우스 hover/pressed 상태, 키보드 좌/우 이동, Focus 표시

2. Slanted 스타일 (스타일 1)
   - 탭 모양: 좌상단 라운드 6px, 오른쪽 변은 하단으로 16px 벌어지는 사선 (사다리꼴)
   - 탭끼리 12px 겹침. 그리기 순서: 비선택 탭을 오른쪽→왼쪽으로, 선택 탭은 마지막에
   - 비선택 탭 높이 28px, hover 31px, 선택 34px (하단 정렬)
   - 채우기: AccentColor 기반 세로 LinearGradientBrush(위쪽 20% 밝게), 회색 탭은 #F6F7F9→#E2E5E9
   - 오른쪽 끝에 검색 아이콘 툴버튼, + 툴버튼(파란 그라데이션)

3. Rounded 스타일 (스타일 2)
   - 상단 라운드 6px 사각 탭, 간격 2px, 텍스트 대문자
   - 비선택: 흰색→#F6F7F9 그라데이션 + #E6E9ED 테두리, 선택: #62AEE9→#2F7FC8, 흰 글자
   - 오른쪽에 상단 라운드 8px 검색 박스, 안에 BorderStyle.None TextBox를 자식으로 배치, 원형 X 지우기 버튼

4. SubMenuBar : Control
   - 항목: Icon(Image 또는 글리프), Title(대문자, Bold), Caption(회색 작은 글씨)
   - 항목 폭은 균등 분배, 사이 1px 세로 구분선, hover 배경, 선택 항목 표시
   - 상단 3px 띠 색 = 선택된 탭의 AccentColor(회색 탭이면 #4A5462), Rounded 스타일이면 2px #2F7FC8
   - 하단 그림자는 아래 2~4px 반투명 그라데이션으로 표현
   - 이벤트: ItemClick(SubItem)

5. MainForm : XtraForm
   - FormBorderStyle.None, 커스텀 타이틀바(높이 34, 최소/최대/닫기 버튼), WM_NCHITTEST로 드래그·가장자리 8px 리사이즈
   - StyledTabStrip(Top) → SubMenuBar(Top) → XtraTabControl(Fill, ShowTabHeader=False)
   - 탭 선택 시 SubMenuBar 항목 교체 + XtraTabControl 페이지 전환
   - 스타일 1/2 전환 데모 버튼

[조건]
- 색, 높이, 패딩은 모두 TabStripTheme 클래스 상수로 분리
- DPI 대응: DeviceDpi 기준 스케일 함수 사용
- 텍스트는 TextRenderer, 도형은 SmoothingMode.AntiAlias
- GDI 객체(Brush, Pen, Path)는 using으로 해제
```
