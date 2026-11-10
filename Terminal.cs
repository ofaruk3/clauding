using System.Diagnostics;
using Microsoft.Win32;

namespace Clauding;

/// <summary>Which shells and terminals exist on this PC, and how a new Claude window is started.</summary>
static class Terminal
{
    public record Kabuk(string id, string ad, string? yol);

    /// <summary>First match of an executable on PATH (plus a few known install folders).</summary>
    public static string? Bul(string exe, params string[] ekYerler)
    {
        foreach (var y in ekYerler)
        {
            var p = Environment.ExpandEnvironmentVariables(y);
            if (File.Exists(p)) return p;
        }
        foreach (var d in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try { var p = Path.Combine(Environment.ExpandEnvironmentVariables(d), exe); if (File.Exists(p)) return p; } catch { }
        }
        return null;
    }

    public static string? ClaudeYolu() =>
        Bul("claude.exe", @"%USERPROFILE%\.local\bin\claude.exe") ?? Bul("claude.cmd") ?? Bul("claude");

    public static string? WtYolu() => Bul("wt.exe", @"%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe");

    public static string? WebView2Surumu()
    {
        try { return Microsoft.Web.WebView2.Core.CoreWebView2Environment.GetAvailableBrowserVersionString(); } catch { return null; }
    }

    static string? GitBash()
    {
        var git = Bul("git.exe");
        var adaylar = new List<string> { @"%ProgramFiles%\Git\bin\bash.exe", @"%ProgramFiles(x86)%\Git\bin\bash.exe", @"%LOCALAPPDATA%\Programs\Git\bin\bash.exe" };
        if (git != null) adaylar.Insert(0, Path.GetFullPath(Path.Combine(Path.GetDirectoryName(git)!, "..", "bin", "bash.exe")));
        return adaylar.Select(Environment.ExpandEnvironmentVariables).FirstOrDefault(File.Exists);
    }

    static bool WslDagitimVar()
    {
        try
        {
            if (!File.Exists(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\wsl.exe"))) return false;
            using var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Lxss");
            return k != null && k.GetSubKeyNames().Length > 0;
        }
        catch { return false; }
    }

    /// <summary>Every supported shell; yol == null means not installed.</summary>
    public static List<Kabuk> Kabuklar() => new()
    {
        new("pwsh", "PowerShell 7", Bul("pwsh.exe", @"%ProgramFiles%\PowerShell\7\pwsh.exe")),
        new("powershell", "Windows PowerShell", Bul("powershell.exe", @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe")),
        new("cmd", "Command Prompt", Bul("cmd.exe", @"%SystemRoot%\System32\cmd.exe")),
        new("gitbash", "Git Bash", GitBash()),
        new("wsl", "WSL", WslDagitimVar() ? Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\wsl.exe") : null),
    };

    /// <summary>The command line that runs Claude inside the chosen shell and keeps the shell open afterwards.</summary>
    static string KabukKomutu(string kabuk, string komut, string klasor, bool wtIcin)
    {
        // Windows Terminal splits its own command line on ';' — escape it there
        string nk = wtIcin ? "\\;" : ";";
        switch (kabuk)
        {
            case "pwsh": return $"pwsh -NoLogo -NoExit -Command {komut}";
            case "cmd": return $"cmd /k {komut}";
            case "gitbash":
                var bash = GitBash() ?? "bash.exe";
                return $"\"{bash}\" --login -i -c \"{komut}{nk} exec bash --login -i\"";
            case "wsl": return $"wsl.exe --cd \"{klasor}\" -- bash -lic \"{komut}{nk} exec bash -l\"";
            default: return $"powershell -NoLogo -NoExit -Command {komut}";
        }
    }

    /// <summary>Starts a new Claude window. Returns false (and the reason) when it could not.</summary>
    public static bool Ac(Ayarlar a, string klasor, out string hata)
    {
        hata = "";
        try
        {
            if (!Directory.Exists(klasor)) { hata = Metin.T("klasorYok", klasor); return false; }
            bool wt = a.Terminal == "wt" && WtYolu() != null;
            if (wt)
            {
                var arg = $"-w new {(a.OdakModu ? "--focus " : "")}new-tab -d \"{klasor}\" {KabukKomutu(a.Kabuk, a.Komut, klasor, true)}";
                Process.Start(new ProcessStartInfo("wt.exe", arg) { UseShellExecute = true });
            }
            else
            {
                // conhost.exe forces the classic console window even where Windows Terminal is the default terminal
                KonsolBekle();
                Process.Start(new ProcessStartInfo("conhost.exe", KabukKomutu(a.Kabuk, a.Komut, klasor, false)) { UseShellExecute = false, WorkingDirectory = klasor });
            }
            Kayit.Yaz($"new Claude: {(wt ? "wt" : "conhost")} {a.Kabuk} in {klasor}");
            return true;
        }
        catch (Exception e) { hata = e.Message; Kayit.Yaz("launch failed: " + e); return false; }
    }

    // ---------- classic console windows: only the ones we started (or that report through a hook) are managed ----------

    static readonly object kilit = new();
    static int beklenen;
    static DateTime sonBekleme;

    static void KonsolBekle() { lock (kilit) { beklenen++; sonBekleme = DateTime.Now; } }

    /// <summary>A console window just appeared: is it one we are waiting for?</summary>
    public static bool BeklenenKonsol()
    {
        lock (kilit)
        {
            if (beklenen <= 0 || (DateTime.Now - sonBekleme).TotalSeconds > 8) { beklenen = 0; return false; }
            beklenen--; return true;
        }
    }
}
