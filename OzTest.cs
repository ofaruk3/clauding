using System.Text;

namespace Clauding;

/// <summary>
/// `Clauding.exe --self-test [report.txt]`: checks the parts that need no windows (layout plans, shortcut
/// parsing, hook install/remove on a temp copy, settings round-trip). Exit code 0 = everything passed.
/// </summary>
static class OzTest
{
    public static int Calistir(string? rapor)
    {
        var sb = new StringBuilder();
        int hata = 0;
        void Bak(bool ok, string ne) { sb.AppendLine((ok ? "PASS " : "FAIL ") + ne); if (!ok) hata++; }

        // layout plans
        string P(Plan p) => (p.Sutunlu ? "c:" : "r:") + string.Join(",", p.Ikincil);
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 1, "auto", false, "rows", 1100, 2, 18)) == "r:1", "1 window = one tile");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 3, "auto", false, "rows", 1100, 2, 18)) == "r:3", "auto 3 = one row");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 5, "auto", false, "rows", 1100, 2, 18)) == "r:3,2", "auto 5 = 3 + 2");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 6, "cols", false, "rows", 1100, 2, 18)) == "r:6", "columns 6 = one row");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 6, "cols", true, "rows", 1100, 2, 18)) == "r:2,2,2", "rule: narrow → 2 per row");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 3, "cols", true, "rows", 1100, 2, 18)) == "r:3", "rule: wide enough → unchanged");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 4, "main", false, "rows", 1100, 2, 18)) == "c:1,3", "main + stack 4");
        Bak(P(Yonetici.PlanHesapla(3800, 2000, 7, "main", false, "rows", 1100, 2, 18)) == "c:1,3,3", "main + two stacks 7");
        Bak(P(Yonetici.PlanHesapla(1040, 1800, 3, "auto", false, "rows", 1100, 2, 18)) == "r:1,1,1", "portrait simple = rows");
        Bak(P(Yonetici.PlanHesapla(1040, 1800, 6, "cols", true, "main", 1100, 2, 18)) == "c:1,3,2", "portrait advanced uses its own preset");

        // shortcuts
        Bak(Ayarlar.KisayolCoz("Ctrl+Win+Alt+N", out var m1, out var v1) && v1 == (uint)Keys.N && (m1 & (N.MOD_CONTROL | N.MOD_WIN | N.MOD_ALT)) == (N.MOD_CONTROL | N.MOD_WIN | N.MOD_ALT), "parse Ctrl+Win+Alt+N");
        Bak(Ayarlar.KisayolCoz("Ctrl+Win+Alt+OemPeriod", out _, out var v2) && v2 == (uint)Keys.OemPeriod, "parse OemPeriod");
        Bak(Ayarlar.KisayolCoz("Ctrl+Win+Alt+3", out _, out var v3) && v3 == (uint)Keys.D3, "parse digit");
        Bak(!Ayarlar.KisayolCoz("Ctrl+Win+Alt+Nope", out _, out _), "reject unknown key");
        var varsayilan = Ayarlar.VarsayilanKlavye();
        Bak(varsayilan.Values.Where(v => v != "").Distinct().Count() == varsayilan.Values.Count(v => v != ""), "default shortcuts are unique");
        Bak(varsayilan.Keys.All(k => Katalog.Eylemler.Any(e => e.id == k)), "every default shortcut has a catalog entry");
        Bak(!varsayilan.Values.Any(v => v.Contains("Shift")), "no Ctrl+Win+Alt+Shift (Office key)");

        // hooks on a temp copy
        var gecici = Path.Combine(Path.GetTempPath(), $"clauding-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(gecici);
        try
        {
            var dosya = Path.Combine(gecici, "settings.json");
            File.WriteAllText(dosya, "{\n  \"model\": \"x\",\n  \"not\": \"kısa + <özel>\",\n  \"hooks\": { \"Stop\": [ { \"hooks\": [ { \"type\": \"command\", \"command\": \"echo mine\" } ] } ] }\n}");
            Kancalar.DosyaOverride = dosya;
            Bak(!Kancalar.Kurulu(), "hooks: not installed at first");
            Kancalar.Kur();
            Bak(Kancalar.Kurulu(), "hooks: installed");
            Kancalar.Kur();
            var icerik = File.ReadAllText(dosya);
            Bak(icerik.Split("--signal done").Length == 2, "hooks: installing twice adds nothing");
            Bak(icerik.Contains("echo mine") && icerik.Contains("\"model\""), "hooks: the user's own settings stay");
            Bak(icerik.Contains("kısa + <özel>"), "hooks: non-ASCII text and symbols are written back unescaped");
            Kancalar.Kaldir();
            icerik = File.ReadAllText(dosya);
            Bak(!icerik.Contains("--signal") && icerik.Contains("echo mine"), "hooks: removed, the user's hook stays");
            Bak(File.Exists(dosya + ".clauding-backup"), "hooks: a backup was made");
        }
        catch (Exception e) { Bak(false, "hooks threw: " + e.Message); }
        finally { Kancalar.DosyaOverride = null; try { Directory.Delete(gecici, true); } catch { } }

        // catalog
        Bak(Katalog.Eylemler.Select(e => e.id).Distinct().Count() == Katalog.Eylemler.Length, "catalog ids are unique");
        Bak(Katalog.Eylemler.All(e => e.en != "" && e.tr != ""), "catalog: every action has both languages");

        // changelog: every release needs its notes (they become the GitHub release notes too)
        try
        {
            using var cl = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "web", "changelog.json")));
            var ilk = cl.RootElement.GetProperty("versions")[0];
            Bak(ilk.GetProperty("version").GetString() == Surum.Simdiki, $"changelog: top entry is {Surum.Simdiki}");
            Bak(ilk.GetProperty("en").GetArrayLength() > 0 && ilk.GetProperty("tr").GetArrayLength() == ilk.GetProperty("en").GetArrayLength(), "changelog: English and Turkish notes match");
        }
        catch (Exception e) { Bak(false, "changelog unreadable: " + e.Message); }

        sb.AppendLine(hata == 0 ? "ALL PASSED" : $"{hata} FAILED");
        if (rapor != null) File.WriteAllText(rapor, sb.ToString()); else Console.Write(sb.ToString());
        return hata == 0 ? 0 : 1;
    }
}
