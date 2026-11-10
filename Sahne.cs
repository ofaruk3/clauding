using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Clauding;

/// <summary>
/// The backdrop behind the tiled terminals: a borderless, non-activating window hosting the HTML scene
/// (web/index.html). It sits directly below the terminals in z-order; what shows through the gaps
/// between them is this page (headers, corner marks, dividers, the work-area edges).
/// </summary>
sealed class Sahne : Form
{
    public static readonly Color Zemin = Color.FromArgb(18, 18, 17);
    readonly WebView2 web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Zemin };
    bool hazir;
    string? bekleyenDurum;
    public bool Ikinci { get; }
    public event Action<JsonElement>? Mesaj;
    public event Action? Tiklandi;

    public Sahne(bool ikinci)
    {
        Ikinci = ikinci;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Zemin;
        Controls.Add(web);
        _ = Baslat();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00000080 /*TOOLWINDOW: no Alt-Tab*/ | 0x08000000 /*NOACTIVATE*/;
            return cp;
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_MOUSEACTIVATE = 0x0021, MA_NOACTIVATE = 3;
        if (m.Msg == WM_MOUSEACTIVATE) { m.Result = new IntPtr(MA_NOACTIVATE); return; }
        base.WndProc(ref m);
    }

    protected override void OnActivated(EventArgs e) { base.OnActivated(e); Tiklandi?.Invoke(); }

    async Task Baslat()
    {
        try
        {
            var env = await WebOrtam.Al();
            await web.EnsureCoreWebView2Async(env);
            var c = web.CoreWebView2;
            c.Settings.AreDefaultContextMenusEnabled = false;
            c.Settings.AreDevToolsEnabled = true;
            c.Settings.IsStatusBarEnabled = false;
            c.Settings.IsZoomControlEnabled = false;
            c.Settings.AreBrowserAcceleratorKeysEnabled = false;
            c.SetVirtualHostNameToFolderMapping("sahne.local", Path.Combine(AppContext.BaseDirectory, "web"), CoreWebView2HostResourceAccessKind.Allow);
            c.WebMessageReceived += (_, a) =>
            {
                try
                {
                    using var d = JsonDocument.Parse(a.WebMessageAsJson);
                    var el = d.RootElement.Clone();
                    if (el.TryGetProperty("t", out var t) && t.GetString() == "hazir") { hazir = true; if (bekleyenDurum != null) Gonder(bekleyenDurum); return; }
                    Mesaj?.Invoke(el);
                }
                catch { }
            };
            c.Navigate("https://sahne.local/index.html" + (Ikinci ? "?ikinci=1" : ""));
        }
        catch (Exception ex)
        {
            Kayit.Yaz("scene could not open: " + ex.Message);
        }
    }

    public void Durum(string json)
    {
        if (!hazir) { bekleyenDurum = json; return; }
        Gonder(json);
    }

    void Gonder(string json) { try { web.CoreWebView2?.PostWebMessageAsJson(json); } catch { } }

    /// <summary>Move/resize without activating or touching z-order.</summary>
    public void Yerles(Rectangle r)
    {
        if (!Visible) { Bounds = r; Show(); }
        N.SetWindowPos(Handle, IntPtr.Zero, r.X, r.Y, r.Width, r.Height, N.SWP_NOZORDER | N.SWP_NOACTIVATE | N.SWP_NOOWNERZORDER);
    }
}
