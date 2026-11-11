using System.Runtime.InteropServices;

namespace Clauding;

/// <summary>
/// One taskbar button for the whole group. Tiled terminals are taken off the taskbar (ITaskbarList.DeleteTab,
/// nothing about the window itself changes) and this window stands in for them.
///
/// It is kept MINIMIZED for its whole life. A click on a minimized window's taskbar button is a request to
/// restore it, and Windows asks the window first (WM_QUERYOPEN). We answer "no" and treat the request as the
/// click. Activation is never used as the signal: Windows activates windows for many reasons of its own
/// (whatever is next in line when another window minimizes or closes), and reacting to those made the group
/// hide itself again in a loop. A minimized window is also never "next in line".
/// </summary>
sealed class GorevDugmesi : Form
{
    public event Action? Tiklandi;
    /// <summary>Picture for the taskbar hover preview, fitted inside the given size (null = icon).</summary>
    public Func<Size, Bitmap?>? Onizleme;
    /// <summary>Full-size picture for "peek" (hovering the preview), the size of the work area.</summary>
    public Func<Size, Bitmap?>? TamOnizleme;
    Rectangle alan = new(0, 0, 800, 600);
    DateTime sonTik;

    public GorevDugmesi()
    {
        Text = "Clauding";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.Manual;
        Bounds = new Rectangle(-30000, -30000, 1, 1);
        Opacity = 0; // never meant to be seen, even if something restores it behind our back
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "icon.ico")); } catch { }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int evet = 1; // previews come from us (a picture of the group), not from this window
        N.DwmSetWindowAttribute(Handle, 7 /*DWMWA_FORCE_ICONIC_REPRESENTATION*/, ref evet, 4);
        N.DwmSetWindowAttribute(Handle, 10 /*DWMWA_HAS_ICONIC_BITMAP*/, ref evet, 4);
    }

    protected override bool ShowWithoutActivation => true;

    // minimize box + system menu: the taskbar only minimizes/restores windows that have them
    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.Style |= 0x00020000 /*WS_MINIMIZEBOX*/ | 0x00080000 /*WS_SYSMENU*/;
            cp.ExStyle |= 0x00080000 /*WS_EX_LAYERED*/ | 0x00000020 /*WS_EX_TRANSPARENT: click-through*/;
            return cp;
        }
    }

    /// <summary>Show on the taskbar: minimized, without taking focus.</summary>
    public void Ac()
    {
        if (!Visible) Show();
        if (!N.IsIconic(Handle)) N.ShowWindow(Handle, 7 /*SW_SHOWMINNOACTIVE*/);
        Yerles(alan);
    }

    /// <summary>Where the group is: peek draws the full-size picture at the window's restore position.</summary>
    public void Yerles(Rectangle r)
    {
        alan = r;
        if (!IsHandleCreated) return;
        var wp = new WINDOWPLACEMENT { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
        if (!GetWindowPlacement(Handle, ref wp)) return;
        var ws = Screen.PrimaryScreen!.WorkingArea; // rcNormalPosition is in workspace coordinates
        wp.rcNormalPosition = new RECT { L = r.Left - ws.Left, T = r.Top - ws.Top, R = r.Right - ws.Left, B = r.Bottom - ws.Top };
        wp.showCmd = N.IsIconic(Handle) ? 7 /*SW_SHOWMINNOACTIVE*/ : 4 /*SW_SHOWNOACTIVATE*/;
        SetWindowPlacement(Handle, ref wp);
    }

    /// <summary>Tell the taskbar the preview is stale; it asks again on the next hover.</summary>
    public void OnizlemeEskidi() { if (IsHandleCreated) G.DwmInvalidateIconicBitmaps(Handle); }

    void Tik()
    {
        // one click can arrive as more than one request (restore + syscommand); a person cannot click twice in 250 ms
        if ((DateTime.Now - sonTik).TotalMilliseconds < 250) return;
        sonTik = DateTime.Now;
        BeginInvoke(() => Tiklandi?.Invoke()); // after the taskbar's own call has returned
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_QUERYOPEN = 0x0013, WM_SYSCOMMAND = 0x0112, SC_MINIMIZE = 0xF020, SC_RESTORE = 0xF120, SC_MAXIMIZE = 0xF030;
        const int WM_DWMSENDICONICTHUMBNAIL = 0x0323, WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326;
        if (Environment.GetEnvironmentVariable("CLAUDING_MSGLOG") == "1" && m.Msg is 0x0013 or 0x0112 or 0x0006 or 0x0018 or 0x0046 or 0x001C or 0x0086 or 0x0005 or 0x0313 or 0x0007)
            Kayit.Yaz($"proxy msg 0x{m.Msg:X4} w=0x{m.WParam.ToInt64():X} l=0x{m.LParam.ToInt64():X}");
        switch (m.Msg)
        {
            case WM_QUERYOPEN: // "restore this minimized window": stay minimized, it was a click
                Tik(); m.Result = IntPtr.Zero; return;
            case WM_SYSCOMMAND:
                int sc = m.WParam.ToInt32() & 0xFFF0;
                if (sc is SC_RESTORE or SC_MINIMIZE or SC_MAXIMIZE) { Tik(); return; }
                break;
            case WM_DWMSENDICONICTHUMBNAIL:
            {
                long l = m.LParam.ToInt64();
                Resim(Onizleme?.Invoke(new Size((int)((l >> 16) & 0xFFFF), (int)(l & 0xFFFF))), canli: false);
                return;
            }
            case WM_DWMSENDICONICLIVEPREVIEWBITMAP:
                Resim(TamOnizleme?.Invoke(alan.Size), canli: true);
                return;
        }
        base.WndProc(ref m);
    }

    void Resim(Bitmap? bmp, bool canli)
    {
        if (Environment.GetEnvironmentVariable("CLAUDING_MSGLOG") == "1") Kayit.Yaz($"preview request ({(canli ? "peek" : "thumbnail")}): {(bmp == null ? "none" : $"{bmp.Width}x{bmp.Height}")}");
        if (bmp == null) return;
        try
        {
            var hb = bmp.GetHbitmap(Color.FromArgb(0));
            if (canli) G.DwmSetIconicLivePreviewBitmap(Handle, hb, IntPtr.Zero, 0);
            else G.DwmSetIconicThumbnail(Handle, hb, 0);
            G.DeleteObject(hb);
        }
        catch (Exception e) { Kayit.Yaz("preview failed: " + e.Message); }
        finally { bmp.Dispose(); }
    }

    [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    struct WINDOWPLACEMENT { public int length, flags, showCmd; public POINT ptMin, ptMax; public RECT rcNormalPosition; }
    [DllImport("user32.dll")] static extern bool GetWindowPlacement(IntPtr h, ref WINDOWPLACEMENT wp);
    [DllImport("user32.dll")] static extern bool SetWindowPlacement(IntPtr h, ref WINDOWPLACEMENT wp);
}

/// <summary>Screen capture for the preview, scaled down by GDI while copying (cheap even at 4K).</summary>
static class G
{
    [DllImport("dwmapi.dll")] public static extern int DwmSetIconicThumbnail(IntPtr h, IntPtr hbmp, uint flags);
    [DllImport("dwmapi.dll")] public static extern int DwmInvalidateIconicBitmaps(IntPtr h);
    [DllImport("dwmapi.dll")] public static extern int DwmSetIconicLivePreviewBitmap(IntPtr h, IntPtr hbmp, IntPtr ptClient, uint flags);
    [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr o);
    [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
    [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
    [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr dc, int mode);
    [DllImport("gdi32.dll")] static extern bool StretchBlt(IntPtr d, int x, int y, int w, int h, IntPtr s, int sx, int sy, int sw, int sh, uint rop);

    public static Bitmap Yakala(Rectangle alan, int genislik)
    {
        int w = Math.Min(genislik, alan.Width), h = Math.Max(1, (int)Math.Round(alan.Height * (double)w / alan.Width));
        var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        var ekran = GetDC(IntPtr.Zero);
        var hdc = g.GetHdc();
        try { SetStretchBltMode(hdc, 4 /*HALFTONE*/); StretchBlt(hdc, 0, 0, w, h, ekran, alan.X, alan.Y, alan.Width, alan.Height, 0x00CC0020 /*SRCCOPY*/); }
        finally { g.ReleaseHdc(hdc); ReleaseDC(IntPtr.Zero, ekran); }
        return bmp;
    }

    public static Bitmap Boyutla(Bitmap kaynak, Size boyut)
    {
        var bmp = new Bitmap(Math.Max(1, boyut.Width), Math.Max(1, boyut.Height), System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(kaynak, 0, 0, bmp.Width, bmp.Height);
        return bmp;
    }

    /// <summary>Fit inside max (keeping the aspect), opaque.</summary>
    public static Bitmap Sigdir(Bitmap kaynak, Size max)
    {
        double k = Math.Min(max.Width / (double)kaynak.Width, max.Height / (double)kaynak.Height);
        int w = Math.Max(1, (int)(kaynak.Width * k)), h = Math.Max(1, (int)(kaynak.Height * k));
        var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.DrawImage(kaynak, 0, 0, w, h);
        return bmp;
    }
}

static class Gorev
{
    [ComImport, Guid("56FDF342-FD6D-11d0-958A-006097C9A090"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ITaskbarList
    {
        void HrInit();
        void AddTab(IntPtr hwnd);
        void DeleteTab(IntPtr hwnd);
        void ActivateTab(IntPtr hwnd);
        void SetActiveAlt(IntPtr hwnd);
    }

    [ComImport, Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    class TaskbarList { }

    static ITaskbarList? liste;

    static ITaskbarList? Liste()
    {
        if (liste != null) return liste;
        try { var l = (ITaskbarList)new TaskbarList(); l.HrInit(); liste = l; }
        catch (Exception e) { Kayit.Yaz("taskbar list unavailable: " + e.Message); }
        return liste;
    }

    public static void Gizle(IntPtr h) { try { Liste()?.DeleteTab(h); } catch { } }
    public static void Goster(IntPtr h) { try { if (N.IsWindow(h)) Liste()?.AddTab(h); } catch { } }
}
