using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Clauding;

/// <summary>
/// A slim handle above a pulled-out terminal. Focus-mode terminals have no title bar, so this is what
/// you grab to move them. Buttons: put back into the grid, maximise.
/// </summary>
sealed class Tutamak : Form
{
    public IntPtr Hedef { get; }
    public event Action? GeriTak;
    public event Action? Buyut;
    string baslik = "";
    Point? tutma;          // cursor offset while dragging
    Rectangle geriAlani, buyutAlani;
    int ustteki = -1;      // 0 = geri tak, 1 = büyüt (hover)
    static readonly Font Yazi = YaziYukle(10.5f, FontStyle.Bold), Kucuk = YaziYukle(9f, FontStyle.Regular);
    const int Yukseklik = 34;

    public Tutamak(IntPtr hedef)
    {
        Hedef = hedef;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(24, 24, 23);
        DoubleBuffered = true;
        Cursor = Cursors.SizeAll;
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x00000080 /*TOOLWINDOW*/ | 0x08000000 /*NOACTIVATE*/; return cp; }
    }

    static Font YaziYukle(float pt, FontStyle st)
    {
        try
        {
            var pfc = new PrivateFontCollection();
            pfc.AddFontFile(Path.Combine(AppContext.BaseDirectory, "web", "fonts", "BricolageGrotesque.ttf"));
            return new Font(pfc.Families[0], pt, st);
        }
        catch { return new Font("Segoe UI", pt, st); }
    }

    public void Baslik(string s) { if (s != baslik) { baslik = s; Invalidate(); } }

    /// <summary>Sit just above the target window (inside its top if there is no room), right above it in z-order.</summary>
    public void Yerles()
    {
        if (tutma != null) return;
        if (!N.IsWindow(Hedef) || N.IsIconic(Hedef) || N.IsZoomed(Hedef) || !N.IsWindowVisible(Hedef)) { if (Visible) Hide(); return; }
        var f = N.GorunenCerceve(Hedef);
        var ekran = Screen.FromRectangle(f).WorkingArea;
        int y = f.Y - Yukseklik - 4 >= ekran.Y ? f.Y - Yukseklik - 4 : f.Y;
        var r = new Rectangle(f.X, y, f.Width, Yukseklik);
        if (!Visible) { Bounds = r; Show(); }
        var ust = N.GetWindow(Hedef, N.GW_HWNDPREV);
        uint fl = N.SWP_NOACTIVATE | N.SWP_SHOWWINDOW;
        IntPtr after = IntPtr.Zero;
        if (ust == Handle) fl |= N.SWP_NOZORDER;
        else if (ust != IntPtr.Zero && (N.GetWindowLong(ust, N.GWL_EXSTYLE) & N.WS_EX_TOPMOST) == 0) after = ust;
        N.SetWindowPos(Handle, after, r.X, r.Y, r.Width, r.Height, fl);
        int yuvarlak = 3; // DWMWCP_ROUNDSMALL
        N.DwmSetWindowAttribute(Handle, N.DWMWA_WINDOW_CORNER_PREFERENCE, ref yuvarlak, 4);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        // grip dots, title, then the two buttons on the right
        using var nokta = new SolidBrush(Color.FromArgb(110, 106, 99));
        for (int i = 0; i < 3; i++) for (int j = 0; j < 2; j++) g.FillEllipse(nokta, 14 + j * 6, 10 + i * 6, 3, 3);
        using var turuncu = new SolidBrush(Color.FromArgb(255, 90, 54));
        g.FillEllipse(turuncu, 34, Yukseklik / 2 - 3, 6, 6);
        var metinAlani = new RectangleF(48, 0, Width - 48 - 210, Yukseklik);
        TextRenderer.DrawText(g, baslik, Yazi, Rectangle.Round(metinAlani), Color.FromArgb(241, 237, 230),
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        geriAlani = new Rectangle(Width - 200, 0, 108, Yukseklik);
        buyutAlani = new Rectangle(Width - 88, 0, 76, Yukseklik);
        Dugme(g, geriAlani, Metin.T("geriTak"), ustteki == 0);
        Dugme(g, buyutAlani, Metin.T("buyutK"), ustteki == 1);
    }

    static void Dugme(Graphics g, Rectangle r, string s, bool ustunde)
    {
        TextRenderer.DrawText(g, s, Kucuk, r, ustunde ? Color.FromArgb(241, 237, 230) : Color.FromArgb(150, 145, 136),
            TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPrefix);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (tutma is Point t)
        {
            var p = Control.MousePosition;
            var f = N.GorunenCerceve(Hedef);
            int yeniX = p.X - t.X, yeniY = p.Y - t.Y + Height + 4;
            var (l, tp, rr, b) = N.Kenarlar(Hedef);
            N.SetWindowPos(Hedef, IntPtr.Zero, yeniX - l, yeniY - tp, f.Width + l + rr, f.Height + tp + b, N.SWP_NOZORDER | N.SWP_NOACTIVATE);
            N.SetWindowPos(Handle, IntPtr.Zero, yeniX, yeniY - Height - 4, Width, Height, N.SWP_NOZORDER | N.SWP_NOACTIVATE);
            return;
        }
        int u = geriAlani.Contains(e.Location) ? 0 : buyutAlani.Contains(e.Location) ? 1 : -1;
        Cursor = u >= 0 ? Cursors.Hand : Cursors.SizeAll;
        if (u != ustteki) { ustteki = u; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (ustteki != -1) { ustteki = -1; Invalidate(); } }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        if (geriAlani.Contains(e.Location)) { GeriTak?.Invoke(); return; }
        if (buyutAlani.Contains(e.Location)) { Buyut?.Invoke(); return; }
        N.SetForegroundWindow(Hedef);
        var p = Control.MousePosition;
        tutma = new Point(p.X - Left, p.Y - Top);
        Capture = true;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (tutma == null) return;
        tutma = null; Capture = false;
        Yerles();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (!geriAlani.Contains(e.Location) && !buyutAlani.Contains(e.Location)) Buyut?.Invoke();
    }
}
