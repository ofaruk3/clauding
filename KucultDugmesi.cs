using System.Drawing.Drawing2D;

namespace Clauding;

/// <summary>
/// Round floating button in the top-right corner of a maximised terminal: one click puts it back
/// into the grid. Sits just above its window in z-order, never steals focus.
/// </summary>
sealed class KucultDugmesi : Form
{
    public IntPtr Hedef { get; }
    public event Action? Tiklandi;
    bool ustunde;
    const int Boy = 44, Pay = 18;

    public KucultDugmesi(IntPtr hedef)
    {
        Hedef = hedef;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(30, 30, 29);
        DoubleBuffered = true;
        Cursor = Cursors.Hand;
        Size = new Size(Boy, Boy);
        using var yol = new GraphicsPath();
        yol.AddEllipse(0, 0, Boy, Boy);
        Region = new Region(yol);
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x00000080 /*TOOLWINDOW*/ | 0x08000000 /*NOACTIVATE*/; return cp; }
    }

    public void Yerles()
    {
        if (!N.IsWindow(Hedef) || !N.IsZoomed(Hedef) || N.IsIconic(Hedef) || !N.IsWindowVisible(Hedef)) { if (Visible) Hide(); return; }
        var f = N.GorunenCerceve(Hedef);
        var ekran = Screen.FromRectangle(f).WorkingArea;
        f.Intersect(ekran);
        int x = f.Right - Boy - Pay, y = f.Top + Pay;
        if (!Visible) { Location = new Point(x, y); Show(); }
        var ust = N.GetWindow(Hedef, N.GW_HWNDPREV);
        uint fl = N.SWP_NOACTIVATE | N.SWP_SHOWWINDOW | N.SWP_NOSIZE;
        IntPtr after = IntPtr.Zero;
        if (ust == Handle) fl |= N.SWP_NOZORDER;
        else if (ust != IntPtr.Zero && (N.GetWindowLong(ust, N.GWL_EXSTYLE) & N.WS_EX_TOPMOST) == 0) after = ust;
        N.SetWindowPos(Handle, after, x, y, 0, 0, fl);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var zemin = new SolidBrush(ustunde ? Color.FromArgb(52, 50, 47) : Color.FromArgb(30, 30, 29)))
            g.FillEllipse(zemin, 0, 0, Boy - 1, Boy - 1);
        using (var halka = new Pen(ustunde ? Color.FromArgb(255, 90, 54) : Color.FromArgb(80, 77, 72), 1.5f))
            g.DrawEllipse(halka, 1, 1, Boy - 3, Boy - 3);
        // "collapse" mark: two corner arrows pointing inwards
        using var p = new Pen(Color.FromArgb(241, 237, 230), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        float c = Boy / 2f;
        g.DrawLines(p, new[] { new PointF(c - 1, c - 8), new PointF(c - 1, c - 1), new PointF(c - 8, c - 1) });
        g.DrawLine(p, c - 1, c - 1, c - 9, c - 9);
        g.DrawLines(p, new[] { new PointF(c + 1, c + 8), new PointF(c + 1, c + 1), new PointF(c + 8, c + 1) });
        g.DrawLine(p, c + 1, c + 1, c + 9, c + 9);
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); ustunde = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); ustunde = false; Invalidate(); }
    protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); if (e.Button == MouseButtons.Left) Tiklandi?.Invoke(); }
}
