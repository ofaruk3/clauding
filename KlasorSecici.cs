using System.Drawing.Drawing2D;

namespace Clauding;

/// <summary>
/// Small "New Claude in…" picker: number keys 1–9 open that folder, arrows + Enter too, Esc closes.
/// Opens near the top of the screen the mouse is on and takes the keyboard so the numbers work at once.
/// </summary>
sealed class KlasorSecici : Form
{
    readonly List<(string ad, string yol)> klasorler;
    int secili;
    public event Action<int>? Secildi; // folder index, or -2 = add a folder
    const int Ust = 44, Satir = 48, Pay = 8;
    static readonly Color Zemin = Color.FromArgb(28, 28, 26), Cizgi = Color.FromArgb(52, 52, 48), Metin1 = Color.FromArgb(242, 240, 234),
        Metin2 = Color.FromArgb(140, 135, 126), Vurgu = Color.FromArgb(255, 106, 56);
    readonly Font baslikF = new("Segoe UI Variable Text", 9.5f), adF = new("Segoe UI Variable Text Semibold", 10.5f), yolF = new("Cascadia Mono", 8.5f), tusF = new("Segoe UI Variable Text", 9f);

    public KlasorSecici(List<(string ad, string yol)> klasorler, int varsayilan)
    {
        this.klasorler = klasorler;
        secili = Math.Clamp(varsayilan, 0, Math.Max(0, klasorler.Count - 1));
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        KeyPreview = true;
        DoubleBuffered = true;
        BackColor = Zemin;
        Size = new Size(420, Ust + (klasorler.Count + 1) * Satir + Pay);
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ExStyle |= 0x00000080; cp.ClassStyle |= 0x00020000; return cp; }
    }

    public void Goster(Rectangle ekran, Point? sagUst = null)
    {
        var dpi = DeviceDpi / 96f;
        Size = new Size((int)(420 * dpi), (int)((Ust + (klasorler.Count + 1) * Satir + Pay) * dpi));
        Location = sagUst is Point p
            ? new Point(Math.Clamp(p.X - Width, ekran.X, ekran.Right - Width), Math.Clamp(p.Y, ekran.Y, ekran.Bottom - Height))
            : new Point(ekran.X + (ekran.Width - Width) / 2, ekran.Y + (int)(ekran.Height * 0.18));
        Show();
        int yuvarlak = 2;
        N.DwmSetWindowAttribute(Handle, N.DWMWA_WINDOW_CORNER_PREFERENCE, ref yuvarlak, 4);
        Activate();
        N.SetForegroundWindow(Handle);
    }

    void Sec(int i) { Hide(); Secildi?.Invoke(i); Close(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int n = e.KeyCode is >= Keys.D1 and <= Keys.D9 ? e.KeyCode - Keys.D1 : e.KeyCode is >= Keys.NumPad1 and <= Keys.NumPad9 ? e.KeyCode - Keys.NumPad1 : -1;
        if (n >= 0 && n < klasorler.Count) { Sec(n); return; }
        switch (e.KeyCode)
        {
            case Keys.Escape: Close(); break;
            case Keys.Enter: Sec(secili == klasorler.Count ? -2 : secili); break;
            case Keys.Down: secili = (secili + 1) % (klasorler.Count + 1); Invalidate(); break;
            case Keys.Up: secili = (secili + klasorler.Count) % (klasorler.Count + 1); Invalidate(); break;
        }
    }

    protected override void OnDeactivate(EventArgs e) { base.OnDeactivate(e); Close(); }

    int SatirAt(Point p)
    {
        var dpi = DeviceDpi / 96f;
        int i = (int)Math.Floor((p.Y / dpi - Ust) / Satir);
        return i >= 0 && i <= klasorler.Count ? i : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); var i = SatirAt(e.Location); if (i >= 0 && i != secili) { secili = i; Invalidate(); } }
    protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); var i = SatirAt(e.Location); if (i >= 0) Sec(i == klasorler.Count ? -2 : i); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var dpi = DeviceDpi / 96f;
        g.ScaleTransform(dpi, dpi);
        float w = Width / dpi;
        using var cizgi = new Pen(Cizgi);
        g.DrawRectangle(cizgi, 0, 0, w - 1, Height / dpi - 1);
        TextRenderer.DrawText(g, global::Clauding.Metin.T("yeniIn"), baslikF, new Point(16, 14), Metin2);
        for (int i = 0; i <= klasorler.Count; i++)
        {
            var r = new RectangleF(Pay, Ust + i * Satir, w - 2 * Pay, Satir - 4);
            if (i == secili)
            {
                using var yol = Yuvarlak(r, 9);
                using var b = new SolidBrush(Color.FromArgb(48, Vurgu));
                g.FillPath(b, yol);
            }
            if (i == klasorler.Count)
            {
                TextRenderer.DrawText(g, "+  " + global::Clauding.Metin.T("klasorEkle"), adF, new Point((int)r.X + 12, (int)r.Y + 13), Metin2);
                continue;
            }
            TextRenderer.DrawText(g, klasorler[i].ad, adF, new Point((int)r.X + 12, (int)r.Y + 5), Metin1);
            TextRenderer.DrawText(g, Kisalt(klasorler[i].yol, 44), yolF, new Point((int)r.X + 12, (int)r.Y + 25), Metin2);
            if (i < 9)
            {
                var k = new RectangleF(r.Right - 34, r.Y + 11, 22, 22);
                using var kp = Yuvarlak(k, 5);
                using var kb = new SolidBrush(Color.FromArgb(38, 38, 35));
                g.FillPath(kb, kp);
                g.DrawPath(cizgi, kp);
                TextRenderer.DrawText(g, (i + 1).ToString(), tusF, Rectangle.Round(k), Metin1, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    static string Kisalt(string s, int n) => s.Length <= n ? s : "…" + s[^(n - 1)..];

    static GraphicsPath Yuvarlak(RectangleF r, float rad)
    {
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
        p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
        p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
        p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
        p.CloseFigure();
        return p;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { baslikF.Dispose(); adF.Dispose(); yolF.Dispose(); tusF.Dispose(); }
        base.Dispose(disposing);
    }
}
