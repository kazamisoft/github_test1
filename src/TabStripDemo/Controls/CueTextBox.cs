using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TabStripDemo.Controls
{
    /// <summary>
    /// 플레이스홀더(회색 안내 문구)를 지원하는 TextBox.
    /// .NET Framework 에는 TextBox.PlaceholderText 가 없으므로 EM_SETCUEBANNER 를 사용한다.
    /// (Application.EnableVisualStyles() 가 호출되어 있어야 표시됨)
    /// </summary>
    public class CueTextBox : TextBox
    {
        private const int EM_SETCUEBANNER = 0x1501;
        private string _cue = string.Empty;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        public string Cue
        {
            get => _cue;
            set
            {
                _cue = value ?? string.Empty;
                ApplyCue();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyCue();
        }

        private void ApplyCue()
        {
            // wParam=1 : 포커스가 있어도 텍스트가 비어 있으면 안내 문구 표시
            if (IsHandleCreated) SendMessage(Handle, EM_SETCUEBANNER, (IntPtr)1, _cue);
        }
    }
}
