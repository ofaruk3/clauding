using System.Runtime.InteropServices;
using System.Text;
using Velopack;

namespace Clauding;

static class Program
{
    public const string MesajPencereAdi = "ClaudingMsg";

    [STAThread]
    static int Main(string[] args)
    {
        // Velopack must run first: it handles install / update / uninstall events and exits on its own when needed.
        // Only the tray app itself may install a downloaded update at startup. Claude Code starts this exe for
        // every hook (--signal ...); if that process installed the update, Velopack would close the running tray
        // app and restart only the hook process (with the hook's arguments), so the app would be gone.
        bool anaUygulama = args.Length == 0, guncellemeSonrasi = false;
        VelopackApp.Build()
            .SetAutoApplyOnStartup(anaUygulama)
            .OnRestarted(_ => guncellemeSonrasi = true)
            .OnBeforeUninstallFastCallback(_ => { try { Kancalar.Kaldir(); BaslangicKaydi.Kapat(); } catch { } })
            .OnAfterUpdateFastCallback(_ => { try { if (Kancalar.Kurulu()) Kancalar.Kur(); } catch { } }) // point hooks at the new exe
            .Run();

        if (args.Length >= 2 && (args[0] == "--signal" || args[0] == "--sinyal"))
        {
            // an older version installed the update from a hook process: Velopack restarted us with the hook's
            // arguments and the tray app is gone. Bring it back (through Explorer, for a clean environment).
            if (guncellemeSonrasi && N.FindWindowEx(N.HWND_MESSAGE, IntPtr.Zero, null, MesajPencereAdi) == IntPtr.Zero)
            {
                try { System.Diagnostics.Process.Start("explorer.exe", $"\"{Environment.ProcessPath}\""); } catch { }
                return 0;
            }
            return Sinyal(args[1]);
        }
        if (args.Length >= 1 && args[0] == "--diagnose") return Teshis(args.Length > 1 ? args[1] : null);
        if (args.Length >= 1 && args[0] == "--self-test") return OzTest.Calistir(args.Length > 1 ? args[1] : null);

        using var tek = new Mutex(true, "Clauding_SingleInstance", out bool ilk);
        if (!ilk)
        {
            // already running: bring its settings forward instead of starting twice
            var m = N.FindWindowEx(N.HWND_MESSAGE, IntPtr.Zero, null, MesajPencereAdi);
            if (m != IntPtr.Zero) Gonder(m, "ayarlar|0");
            return 0;
        }
        Kayit.Kur();
        Kayit.Yaz($"start {Surum.Simdiki} on Windows {Environment.OSVersion.Version}");
        ClaudeIziniTemizle();
        ApplicationConfiguration.Initialize();
        Application.Run(new Yonetici());
        return 0;
    }

    /// <summary>
    /// If this app was started from inside a Claude Code session, it inherits that session's
    /// variables (CLAUDE_CODE_CHILD_SESSION, CLAUDE_CODE_SESSION_ID, CLAUDECODE ...). wt.exe passes
    /// the caller's environment to the new tab, so every new Claude would think it is a child
    /// session and turn transcript saving off. Drop them once, before anything is launched.
    /// </summary>
    static void ClaudeIziniTemizle()
    {
        // Variables a Claude Code tool shell adds (NO_COLOR turns every new Claude's colours off, GIT_EDITOR=true
        // breaks commits...). Drop them unless the user really set them in Windows (user/machine environment).
        string[] aracDegiskenleri = { "NO_COLOR", "GIT_EDITOR", "GIT_TERMINAL_PROMPT", "GIT_ASKPASS", "WT_SESSION", "WT_PROFILE_ID" };
        foreach (System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
        {
            var ad = (string)e.Key;
            if (ad.Equals("CLAUDECODE", StringComparison.OrdinalIgnoreCase)
                || ad.StartsWith("CLAUDE_CODE_", StringComparison.OrdinalIgnoreCase)
                || ad.Equals("CLAUDE_PID", StringComparison.OrdinalIgnoreCase)
                || ad.Equals("CLAUDE_EFFORT", StringComparison.OrdinalIgnoreCase))
                Environment.SetEnvironmentVariable(ad, null);
            else if (aracDegiskenleri.Contains(ad, StringComparer.OrdinalIgnoreCase)
                && Environment.GetEnvironmentVariable(ad, EnvironmentVariableTarget.User) == null
                && Environment.GetEnvironmentVariable(ad, EnvironmentVariableTarget.Machine) == null)
                Environment.SetEnvironmentVariable(ad, null);
        }
    }

    /// <summary>Called by the Claude Code hooks: finds the window this Claude runs in and tells the tray app.</summary>
    static int Sinyal(string durum)
    {
        StdinBosalt();
        try
        {
            durum = durum switch { "waiting" => "izin", "done" => "bitti", "clear" => "temizle", _ => durum };
            var msj = N.FindWindowEx(N.HWND_MESSAGE, IntPtr.Zero, null, MesajPencereAdi);
            if (msj == IntPtr.Zero) return 0; // tray app not running
            var pencere = TerminalBul(out _);
            Gonder(msj, $"{durum}|{pencere.ToInt64()}");
        }
        catch { /* a hook must never fail */ }
        return 0;
    }

    public static void Gonder(IntPtr hedef, string metin)
    {
        var bytes = Encoding.Unicode.GetBytes(metin + "\0");
        var p = Marshal.AllocHGlobal(bytes.Length);
        try
        {
            Marshal.Copy(bytes, 0, p, bytes.Length);
            var cds = new N.COPYDATASTRUCT { dwData = new IntPtr(0x434C), cbData = bytes.Length, lpData = p };
            N.SendMessage(hedef, N.WM_COPYDATA, IntPtr.Zero, ref cds);
        }
        finally { Marshal.FreeHGlobal(p); }
    }

    /// <summary>Claude Code pipes JSON to hooks; read it so the writer never sees a broken pipe.</summary>
    static void StdinBosalt()
    {
        try
        {
            if (!Console.IsInputRedirected) return;
            var t = Task.Run(() => Console.In.ReadToEnd());
            t.Wait(400);
        }
        catch { }
    }

    /// <summary>
    /// The terminal window that hosts the console of this process (or of an ancestor).
    /// Under Windows Terminal, ConPTY's pseudo console window is owned by the terminal window;
    /// in a classic console the console window itself is the one.
    /// </summary>
    public static IntPtr TerminalBul(out string iz)
    {
        var sb = new StringBuilder();
        IntPtr Coz()
        {
            var c = N.GetConsoleWindow();
            if (c == IntPtr.Zero) { sb.Append(" console=0"); return IntPtr.Zero; }
            var kok = N.GetAncestor(c, N.GA_ROOTOWNER);
            sb.Append($" console={c:X}({N.Sinif(c)}) root={kok:X}({N.Sinif(kok)})");
            if (N.Sinif(kok) == N.TerminalSinifi) return kok;
            if (N.Sinif(c) == N.KonsolSinifi && N.IsWindowVisible(c)) return c;
            var h = c;
            for (int i = 0; i < 5 && h != IntPtr.Zero; i++)
            {
                if (N.Sinif(h) == N.TerminalSinifi) return h;
                h = N.GetWindow(h, N.GW_OWNER);
            }
            return IntPtr.Zero;
        }

        // 1) own console (if any), 2) parent's console, 3) every ancestor's console
        var r = Coz();
        if (r != IntPtr.Zero) { iz = sb.ToString(); return r; }
        N.FreeConsole();
        if (N.AttachConsole(-1)) { r = Coz(); N.FreeConsole(); if (r != IntPtr.Zero) { iz = sb.ToString(); return r; } }
        else sb.Append(" no-parent-console");

        var surec = N.Surecler();
        uint pid = (uint)Environment.ProcessId;
        for (int i = 0; i < 12; i++)
        {
            if (!surec.TryGetValue(pid, out var e)) break;
            pid = e.ebeveyn;
            if (!surec.TryGetValue(pid, out var ust)) break;
            sb.Append($" | {ust.ad}:{pid}");
            if (ust.ad.Equals("WindowsTerminal.exe", StringComparison.OrdinalIgnoreCase)) break;
            if (N.AttachConsole((int)pid)) { r = Coz(); N.FreeConsole(); if (r != IntPtr.Zero) { iz = sb.ToString(); return r; } }
        }
        iz = sb.ToString();
        return IntPtr.Zero;
    }

    static int Teshis(string? dosya)
    {
        var sb = new StringBuilder();
        var wt = TerminalBul(out var iz);
        sb.AppendLine($"found terminal: {wt:X} '{N.Baslik(wt)}'");
        sb.AppendLine($"trace: {iz}");
        sb.AppendLine("terminal windows:");
        N.EnumWindows((h, _) =>
        {
            var s = N.Sinif(h);
            if ((s == N.TerminalSinifi || s == N.KonsolSinifi) && N.IsWindowVisible(h))
                sb.AppendLine($"  {h:X} {s} '{N.Baslik(h)}' {N.GorunenCerceve(h)} frame={N.Kenarlar(h)}");
            return true;
        }, IntPtr.Zero);
        File.WriteAllText(dosya ?? Path.Combine(Path.GetTempPath(), "clauding-diagnose.txt"), sb.ToString());
        return 0;
    }
}
