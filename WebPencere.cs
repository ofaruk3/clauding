using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Clauding;

/// <summary>A borderless window with a page from web/ (shortcuts panel, onboarding, settings) and a message bridge.</summary>
class WebPencere : Form
{
    protected readonly WebView2 web = new() { Dock = DockStyle.Fill, DefaultBackgroundColor = Color.FromArgb(14, 14, 13) };
    bool hazir;
    readonly List<string> bekleyen = new();
    readonly string sayfa;
    public event Action<JsonElement>? Mesaj;
    public event Action? Hazir;

    public WebPencere(string sayfa, bool gorevCubugu)
    {
        this.sayfa = sayfa;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = gorevCubugu;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(14, 14, 13);
        Text = "Clauding";
        Controls.Add(web);
        _ = Baslat();
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= 0x00020000 /*CS_DROPSHADOW*/; if (!ShowInTaskbar) cp.ExStyle |= 0x00000080 /*TOOLWINDOW*/; return cp; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int yuvarlak = 2;
        N.DwmSetWindowAttribute(Handle, N.DWMWA_WINDOW_CORNER_PREFERENCE, ref yuvarlak, 4);
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? ""); } catch { }
    }

    async Task Baslat()
    {
        try
        {
            var env = await WebOrtam.Al();
            await web.EnsureCoreWebView2Async(env);
            var c = web.CoreWebView2;
            c.Settings.AreDefaultContextMenusEnabled = false;
            c.Settings.IsStatusBarEnabled = false;
            c.Settings.IsZoomControlEnabled = false;
            c.Settings.AreBrowserAcceleratorKeysEnabled = false;
            c.SetVirtualHostNameToFolderMapping("sahne.local", Path.Combine(AppContext.BaseDirectory, "web"), CoreWebView2HostResourceAccessKind.Allow);
            c.NewWindowRequested += (_, a) => { a.Handled = true; Ac(a.Uri); };
            c.WebMessageReceived += (_, a) =>
            {
                try
                {
                    using var d = JsonDocument.Parse(a.WebMessageAsJson);
                    var el = d.RootElement.Clone();
                    var t = el.GetProperty("t").GetString();
                    if (t == "hazir") { hazir = true; foreach (var b in bekleyen) Gonder(b); bekleyen.Clear(); Hazir?.Invoke(); return; }
                    if (t == "tasi") { Tasi(); return; }
                    if (t == "link") { Ac(el.GetProperty("url").GetString()); return; }
                    Mesaj?.Invoke(el);
                }
                catch (Exception e) { Kayit.Yaz($"{sayfa} message error: {e.Message}"); }
            };
            c.Navigate("https://sahne.local/" + sayfa);
        }
        catch (Exception ex) { Kayit.Yaz($"{sayfa} could not open: {ex.Message}"); }
    }

    static void Ac(string? url)
    {
        if (url != null && (url.StartsWith("https://") || url.StartsWith("ms-windows-store:")))
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    public void Gonder(string json)
    {
        if (!hazir) { bekleyen.Add(json); return; }
        try { web.CoreWebView2?.PostWebMessageAsJson(json); } catch { }
    }

    public void Gonder(object o) => Gonder(JsonSerializer.Serialize(o));

    public void Ortala(int w, int h)
    {
        var a = Screen.FromPoint(Cursor.Position).WorkingArea;
        w = Math.Min(w, a.Width - 40); h = Math.Min(h, a.Height - 40);
        Bounds = new Rectangle(a.X + (a.Width - w) / 2, a.Y + (a.Height - h) / 2, w, h);
    }

    /// <summary>Drag the window by its top area (the page asks for it on mouse down there).</summary>
    void Tasi()
    {
        const int WM_NCLBUTTONDOWN = 0xA1, HTCAPTION = 2;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern bool ReleaseCapture();
    [System.Runtime.InteropServices.DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, int w, int l);
}
