using System;
using System.Windows.Forms;

namespace MeshCentralRouter
{
    // Passes hit-testing through so the parent form still sees resize edges under the status strip
    public class TransparentStatusStrip : StatusStrip
    {
        private const int wmNcHitTest = 0x84;
        private const int htTransparent = -1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == wmNcHitTest)
            {
                m.Result = (IntPtr)htTransparent;
                return;
            }
            base.WndProc(ref m);
        }
    }
}
