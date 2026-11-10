using System.Text.Json;
using System.Text.Json.Nodes;

namespace Clauding;

/// <summary>
/// Claude Code status hooks in ~/.claude/settings.json: Notification → waiting, Stop → done,
/// UserPromptSubmit → clear. Our entries are added next to the user's own hooks and found again by
/// the "Clauding.exe" + "--signal" signature, so install / update / uninstall never touch anything else.
/// </summary>
static class Kancalar
{
    static string Dosya => DosyaOverride ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "settings.json");
    static readonly (string olay, string durum)[] Olaylar = { ("Notification", "waiting"), ("Stop", "done"), ("UserPromptSubmit", "clear") };
    // relaxed escaping: the user's own settings (non-ASCII text, "<", "+") are written back as they were, not as \u escapes
    static readonly JsonSerializerOptions Js = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    static string Exe => (Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Clauding.exe")).Replace('\\', '/');

    static bool Bizim(JsonNode? hook) =>
        hook?["command"]?.GetValue<string>() is string c && c.Contains("Clauding.exe", StringComparison.OrdinalIgnoreCase) && c.Contains("--signal");

    static JsonObject Oku()
    {
        try { if (File.Exists(Dosya)) return JsonNode.Parse(File.ReadAllText(Dosya), documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject ?? new JsonObject(); }
        catch (Exception e) { throw new InvalidOperationException("~/.claude/settings.json could not be read: " + e.Message); }
        return new JsonObject();
    }

    static void Yaz(JsonObject kok)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Dosya)!);
        var yedek = Dosya + ".clauding-backup";
        if (File.Exists(Dosya) && !File.Exists(yedek)) File.Copy(Dosya, yedek); // one untouched copy, from before Clauding ever wrote
        var gecici = Dosya + ".tmp";
        File.WriteAllText(gecici, kok.ToJsonString(Js));
        File.Move(gecici, Dosya, true);
    }

    /// <summary>Are our hooks there (for all three events)?</summary>
    public static bool Kurulu()
    {
        try
        {
            var h = Oku()["hooks"] as JsonObject;
            return h != null && Olaylar.All(o => (h[o.olay] as JsonArray)?.Any(e => (e?["hooks"] as JsonArray)?.Any(Bizim) == true) == true);
        }
        catch { return false; }
    }

    /// <summary>Add (or repoint) our hooks. Safe to run again.</summary>
    public static void Kur()
    {
        var kok = Oku();
        TemizleIc(kok);
        var hooks = kok["hooks"] as JsonObject ?? new JsonObject();
        kok["hooks"] = hooks;
        foreach (var (olay, durum) in Olaylar)
        {
            var dizi = hooks[olay] as JsonArray ?? new JsonArray();
            hooks[olay] = dizi;
            dizi.Add(new JsonObject
            {
                ["hooks"] = new JsonArray(new JsonObject
                {
                    ["type"] = "command",
                    ["command"] = $"\"{Exe}\" --signal {durum}",
                    ["timeout"] = 5,
                }),
            });
        }
        Yaz(kok);
        Kayit.Yaz("status hooks installed for " + Exe);
    }

    /// <summary>Remove only our entries; the user's own hooks stay exactly as they were.</summary>
    public static void Kaldir()
    {
        if (!File.Exists(Dosya)) return;
        var kok = Oku();
        if (TemizleIc(kok)) { Yaz(kok); Kayit.Yaz("status hooks removed"); }
    }

    static bool TemizleIc(JsonObject kok)
    {
        if (kok["hooks"] is not JsonObject hooks) return false;
        bool degisti = false;
        foreach (var (olay, _) in Olaylar)
        {
            if (hooks[olay] is not JsonArray dizi) continue;
            for (int i = dizi.Count - 1; i >= 0; i--)
            {
                if (dizi[i]?["hooks"] is not JsonArray ic) continue;
                for (int j = ic.Count - 1; j >= 0; j--) if (Bizim(ic[j])) { ic.RemoveAt(j); degisti = true; }
                if (ic.Count == 0) dizi.RemoveAt(i);
            }
            if (dizi.Count == 0) hooks.Remove(olay);
        }
        if (hooks.Count == 0) kok.Remove("hooks");
        return degisti;
    }

    /// <summary>For the self-test: run install/remove against another file.</summary>
    public static string? DosyaOverride;
}
