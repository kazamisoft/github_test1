using System;
using System.Drawing;
using System.Windows.Forms;
namespace TabStripDemo
{
    /// <summary>
    /// WM_NCHITTEST 에 HTTRANSPARENT 를 돌려줘서 히트 테스트를 부모(폼)로 넘기는 패널.
    /// 타이틀바(드래그·더블클릭 최대화)와 가장자리 리사이즈를 폼 하나에서 처리하기 위해 쓴다.
    /// </summary>
    internal class HitTransparentPanel : Panel
    {
        public HitTransparentPanel()
        {
            DoubleBuffered = true;
        }

        /// <summary>스크린 좌표를 받아 true 이면 폼으로 히트 테스트를 넘긴다.</summary>
        public Func<Point, bool> PassThrough { get; set; }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_NCHITTEST && PassThrough != null &&
                PassThrough(NativeMethods.PointFromLParam(m.LParam)))
            {
                m.Result = (IntPtr)NativeMethods.HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
