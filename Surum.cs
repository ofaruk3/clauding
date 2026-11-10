using Microsoft.Win32;
using Velopack;
using Velopack.Sources;

namespace Clauding;

/// <summary>Version and self-update from GitHub Releases (Velopack). Does nothing when run from a build folder.</summary>
static class Surum
{
    public const string Depo = "https://github.com/ofaruk3/clauding";

    public static string Simdiki =>
        typeof(Surum).Assembly.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "0.0.0";

    static UpdateManager? yonetici;
    static UpdateInfo? bekleyen;
    public static string? HazirSurum => bekleyen?.TargetFullRelease.Version.ToString();

    static UpdateManager? Yonetici()
    {
        try
        {
            yonetici ??= new UpdateManager(new GithubSource(Depo, null, false));
            return yonetici.IsInstalled ? yonetici : null;
        }
        catch (Exception e) { Kayit.Yaz("updater unavailable: " + e.Message); return null; }
    }

    /// <summary>Check, and download in the background if there is something new. Returns the new version or null.</summary>
    public static async Task<string?> DenetleVeIndir()
    {
        var m = Yonetici();
        if (m == null) return null;
        try
        {
            var yeni = await m.CheckForUpdatesAsync();
            if (yeni == null) return null;
            await m.DownloadUpdatesAsync(yeni);
            bekleyen = yeni;
            Kayit.Yaz("update downloaded: " + HazirSurum);
            return HazirSurum;
        }
        catch (Exception e) { Kayit.Yaz("update check failed: " + e.Message); throw; }
    }

    /// <summary>Restart into the downloaded version now.</summary>
    public static void UygulaVeYenidenBaslat()
    {
        var m = Yonetici();
        if (m != null && bekleyen != null) m.ApplyUpdatesAndRestart(bekleyen);
    }

    /// <summary>On a normal quit, install the downloaded version so the next start is already new.</summary>
    public static void CikistaUygula()
    {
        try
        {
            var m = Yonetici();
            if (m != null && bekleyen != null) m.WaitExitThenApplyUpdates(bekleyen, silent: true, restart: false);
        }
        catch (Exception e) { Kayit.Yaz("apply on exit failed: " + e.Message); }
    }

    public static bool Kurulu => Yonetici() != null;
}

/// <summary>"Start with Windows" (HKCU Run).</summary>
static class BaslangicKaydi
{
    const string Anahtar = @"Software\Microsoft\Windows\CurrentVersion\Run", Ad = "Clauding";
    static string Exe => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Clauding.exe");

    public static bool Acik
    {
        get
        {
            using var k = Registry.CurrentUser.OpenSubKey(Anahtar);
            return k?.GetValue(Ad) is string s && s.Contains(Exe, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Ayarla(bool acik)
    {
        try
        {
            using var k = Registry.CurrentUser.OpenSubKey(Anahtar, true)!;
            if (acik) k.SetValue(Ad, $"\"{Exe}\""); else k.DeleteValue(Ad, false);
        }
        catch (Exception e) { Kayit.Yaz("start-with-windows change failed: " + e.Message); }
    }

    public static void Kapat() => Ayarla(false);
}
