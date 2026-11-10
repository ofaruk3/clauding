using System.IO.Compression;

namespace Clauding;

/// <summary>Small rolling log in %LOCALAPPDATA%\Clauding\logs (kept under ~1 MB), plus crash capture.</summary>
static class Kayit
{
    public static string Klasor => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClaudingData", "logs");
    static string Dosya => Path.Combine(Klasor, "clauding.log");
    static readonly object kilit = new();

    public static void Yaz(string s)
    {
        try
        {
            lock (kilit)
            {
                Directory.CreateDirectory(Klasor);
                var f = new FileInfo(Dosya);
                if (f.Exists && f.Length > 1_000_000) File.Move(Dosya, Dosya + ".1", true);
                File.AppendAllText(Dosya, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {s}{Environment.NewLine}");
            }
        }
        catch { }
    }

    public static void Kur()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Yaz("UI error: " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Yaz("fatal: " + e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) => { Yaz("task error: " + e.Exception); e.SetObserved(); };
    }

    /// <summary>Zip the logs and settings to the desktop for a bug report; returns the zip path.</summary>
    public static string? Paketle()
    {
        try
        {
            var hedef = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"clauding-report-{DateTime.Now:yyyyMMdd-HHmm}.zip");
            using var z = ZipFile.Open(hedef, ZipArchiveMode.Create);
            foreach (var f in Directory.Exists(Klasor) ? Directory.GetFiles(Klasor) : Array.Empty<string>())
                z.CreateEntryFromFile(f, "logs/" + Path.GetFileName(f));
            if (File.Exists(Ayarlar.Yol)) z.CreateEntryFromFile(Ayarlar.Yol, "settings.json");
            var e = z.CreateEntry("system.txt");
            using var w = new StreamWriter(e.Open());
            w.WriteLine($"Clauding {Surum.Simdiki}");
            w.WriteLine($"Windows {Environment.OSVersion.Version}");
            w.WriteLine($"screens: {string.Join(", ", Screen.AllScreens.Select(s => $"{s.DeviceName} {s.Bounds.Width}x{s.Bounds.Height}{(s.Primary ? " primary" : "")}"))}");
            return hedef;
        }
        catch (Exception e) { Yaz("report zip failed: " + e.Message); return null; }
    }
}
