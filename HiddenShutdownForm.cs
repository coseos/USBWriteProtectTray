class HiddenShutdownForm : Form
{
    private const int WM_QUERYENDSESSION = 0x0011;
    private const int WM_ENDSESSION = 0x0016;

    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(false);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_QUERYENDSESSION)
        {
            m.Result = (IntPtr)1;
            return;
        }

        if (m.Msg == WM_ENDSESSION && m.WParam != IntPtr.Zero)
        {
            Application.Exit();
        }

        base.WndProc(ref m);
    }
}

