using System.Diagnostics;
using System.Text.Json;

namespace Clauding;

/// <summary>Shortcuts panel, onboarding / settings window and the macro pad upload.</summary>
sealed partial class Yonetici
{
    Panel? panel;
    KurulumPenceresi? kurulum;
    bool kayitModu; // the panel is recording a shortcut: our hotkeys are released meanwhile

    // ---------- shortcuts panel ----------

    void PaneliAc(bool duzenle, bool rehber = false)
    {
        if (panel == null || panel.IsDisposed)
        {
            panel = new Panel();
            panel.Mesaj += PanelMesaji;
        }
        if (panel.Visible && !duzenle && !rehber) { panel.Kapat(); return; } // the shortcut toggles
        panel.Ac(PanelDurumu(duzenle ? "duzen" : "rehber"), rehber);
    }

    string PanelDurumu(string mod)
    {
        var dil = ayar.EtkinDil;
        return JsonSerializer.Serialize(new
        {
            t = "durum", mod, dil,
            makroVar = ayar.MakroKlavye, makro = ayar.Makro, nob = ayar.Nob,
            klavye = ayar.Klavye.ToDictionary(kv => kv.Key, kv => KisayolGoster(kv.Value)),
            ham = ayar.Klavye,
            eylemler = Katalog.Eylemler.Where(e => !e.id.StartsWith("yeni") || e.id == "yeni" || int.TryParse(e.id[4..], out var n) && n <= ayar.Klasorler.Count)
                .Select(e => new { e.id, e.grup, ad = KlasorluAd(e, dil), aciklama = e.Aciklama(dil) }),
            gruplar = Katalog.Gruplar.Select(g => new { g.id, ad = dil == "tr" ? g.tr : g.en }),
            medya = Katalog.Medya.Select(m => new { m.id, ad = m.Ad(dil) }),
            hatali = kisayolHatalari,
            mod2 = Ayarlar.Mod,
        });
    }

    /// <summary>"New Claude in folder 2" → "New Claude in api" once the folder exists.</summary>
    string KlasorluAd(Katalog.Eylem e, string dil)
    {
        if (e.id.Length == 5 && e.id.StartsWith("yeni") && int.TryParse(e.id[4..], out var n) && n <= ayar.Klasorler.Count)
            return (dil == "tr" ? "Yeni Claude: " : "New Claude in ") + ayar.Klasorler[n - 1].Gorunen;
        return e.Ad(dil);
    }

    void PanelMesaji(JsonElement m)
    {
        switch (m.GetProperty("t").GetString())
        {
            case "kayitBasla": kayitModu = true; KisayollariBirak(); break;
            case "kayitBitti": kayitModu = false; KisayollariKaydet(); break;
            case "kisayol":
            {
                // a new combo for an action ("" clears it); another action with the same combo loses it
                var eylem = m.GetProperty("eylem").GetString() ?? "";
                var tus = m.GetProperty("tus").GetString() ?? "";
                string? sonuc = null;
                if (tus != "")
                {
                    var cakisan = ayar.Klavye.FirstOrDefault(kv => kv.Key != eylem && string.Equals(kv.Value, tus, StringComparison.OrdinalIgnoreCase)).Key;
                    KisayollariBirak();
                    if (!KisayolBos(tus)) sonuc = "dolu";
                    else if (cakisan != null) { ayar.Klavye[cakisan] = ""; sonuc = "tasindi:" + cakisan; }
                }
                if (sonuc != "dolu") { ayar.Klavye[eylem] = tus; ayar.Kaydet(); }
                kayitModu = false;
                KisayollariKaydet();
                TepsiMenusunuYenile(); SahneIste();
                panel?.Gonder(PanelDurumu("duzen"));
                panel?.Gonder(new { t = "kayitSonuc", eylem, tus = KisayolGoster(tus), sonuc });
                break;
            }
            case "sifirla":
                ayar.Klavye = Ayarlar.VarsayilanKlavye(); ayar.Kaydet();
                KisayollariKaydet(); TepsiMenusunuYenile(); SahneIste();
                panel?.Gonder(PanelDurumu("duzen"));
                break;
            case "makro":
                var yeni = m.GetProperty("makro").EnumerateArray().Select(x => x.GetString() ?? "bos").ToList();
                if (yeni.Count == 12) { ayar.Makro = yeni; ayar.Kaydet(); KisayollariKaydet(); }
                panel?.Gonder(PanelDurumu("duzen"));
                break;
            case "nob":
                foreach (var p in m.GetProperty("nob").EnumerateObject()) ayar.Nob[p.Name] = p.Value.GetString() ?? "";
                ayar.Kaydet();
                break;
            case "yukle": _ = NoblariYukle(); break;
            case "ayarlar": panel?.Kapat(); AyarlariAc(); break;
            case "kapat": panel?.Kapat(); break;
        }
    }

    /// <summary>Knobs live in the pad: write the YAML and upload it with ch57x-keyboard-tool (buttons stay F13–F24).</summary>
    async Task NoblariYukle()
    {
        var klasor = Path.Combine(AppContext.BaseDirectory, "makro");
        var arac = Path.Combine(klasor, "ch57x-keyboard-tool.exe");
        string Nb(string k) => ayar.Nob.GetValueOrDefault(k, "volumeup");
        var yaml =
            "# 3x4 + 2 knob macro pad, written by Clauding's Shortcuts panel.\n" +
            "# The 12 keys send F13..F24 (Clauding decides what they do). All three layers are the same.\n" +
            "orientation: normal\nrows: 3\ncolumns: 4\nknobs: 2\nlayers:\n" +
            "  - buttons: &tuslar\n" +
            "      - [\"f13\", \"f14\", \"f15\", \"f16\"]\n      - [\"f17\", \"f18\", \"f19\", \"f20\"]\n      - [\"f21\", \"f22\", \"f23\", \"f24\"]\n" +
            "    knobs: &noblar\n" +
            $"      - ccw: \"{Nb("ustSol")}\"\n        press: \"{Nb("ustBas")}\"\n        cw: \"{Nb("ustSag")}\"\n" +
            $"      - ccw: \"{Nb("altSol")}\"\n        press: \"{Nb("altBas")}\"\n        cw: \"{Nb("altSag")}\"\n" +
            "  - buttons: *tuslar\n    knobs: *noblar\n  - buttons: *tuslar\n    knobs: *noblar\n";
        string mesaj; bool ok;
        try
        {
            if (!File.Exists(arac)) throw new FileNotFoundException("ch57x-keyboard-tool.exe is missing");
            var psi = new ProcessStartInfo(arac, "upload")
            {
                RedirectStandardInput = true, RedirectStandardError = true, RedirectStandardOutput = true,
                UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = klasor,
            };
            using var p = Process.Start(psi)!;
            await p.StandardInput.WriteAsync(yaml); p.StandardInput.Close();
            var hata = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            ok = p.ExitCode == 0;
            mesaj = ok ? "ok" : hata.Trim().Split('\n').LastOrDefault() ?? "error";
        }
        catch (Exception e) { ok = false; mesaj = e.Message; }
        Kayit.Yaz($"macro pad upload: {(ok ? "ok" : mesaj)}");
        panel?.Gonder(new { t = "yukleme", ok, mesaj });
    }

    // ---------- onboarding and settings ----------

    void AyarlariAc() => KurulumAc(false);

    void KurulumAc(bool onboarding)
    {
        if (kurulum != null && !kurulum.IsDisposed)
        {
            if (kurulum.Onboarding == onboarding) { kurulum.Show(); kurulum.Activate(); return; }
            kurulum.Close();
        }
        kurulum = new KurulumPenceresi(onboarding);
        kurulum.DurumIste = () => kurulum?.Gonder(KurulumDurumu(onboarding));
        kurulum.Mesaj += m => KurulumMesaji(m, onboarding);
        kurulum.FormClosed += (_, _) =>
        {
            // closing the onboarding window = done with whatever was chosen so far
            if (onboarding && !ayar.OnboardingBitti) { ayar.OnboardingBitti = true; ayar.Kaydet(); Basla(); }
        };
        kurulum.Ortala(onboarding ? 920 : 1080, onboarding ? 700 : 760);
        kurulum.Show();
        kurulum.Activate();
    }

    object KurulumDurumu(bool onboarding)
    {
        var kabuklar = Terminal.Kabuklar();
        return new
        {
            t = "durum",
            mod = onboarding ? "onboarding" : "ayarlar",
            dil = ayar.EtkinDil,
            ayar = new
            {
                dil = ayar.Dil, kabuk = ayar.Kabuk, terminal = ayar.Terminal,
                klasorler = ayar.Klasorler.Select(k => new { yol = k.Yol, ad = k.Gorunen }), varsayilan = ayar.VarsayilanKlasor, klasorSor = ayar.KlasorSor,
                dizilim = ayar.Dizilim, gelismis = ayar.Gelismis, minGenislik = ayar.MinGenislik, satirMaks = ayar.SatirMaks,
                anaOran = ayar.AnaOran, bosluk = ayar.Bosluk, dikeyDizilim = ayar.DikeyDizilim,
                tasma = ayar.AnaEkranKapasite, ikinciEkran = ayar.IkinciEkraniKullan,
                tema = ayar.Tema, vurgu = ayar.Vurgu, efekt = ayar.Efekt, animasyonMs = ayar.AnimasyonMs,
                guncelle = ayar.GuncellemeDenetle, makroKlavye = ayar.MakroKlavye, kapanisMetni = ayar.KapanisMetni,
                odakModu = ayar.OdakModu, rehberBekleme = ayar.RehberBekleme, tekGorevDugmesi = ayar.TekGorevDugmesi,
            },
            algilanan = new
            {
                claude = Terminal.ClaudeYolu(), wt = Terminal.WtYolu() != null, webview2 = Terminal.WebView2Surumu(),
                kabuklar = kabuklar.Select(k => new { k.id, k.ad, var = k.yol != null }),
                ekranlar = Screen.AllScreens.Select(s => new { w = s.Bounds.Width, h = s.Bounds.Height, ana = s.Primary }),
                makro = Donanim.MakroKlavyeVar(),
                kanca = Kancalar.Kurulu(),
                baslangic = BaslangicKaydi.Acik,
                surum = Surum.Simdiki, kurulu = Surum.Kurulu, hazirSurum = Surum.HazirSurum,
                ayarDosyasi = Ayarlar.Yol, kayitKlasoru = Kayit.Klasor,
            },
            kisayol = ayar.Klavye.ToDictionary(kv => kv.Key, kv => KisayolGoster(kv.Value)),
        };
    }

    void KurulumMesaji(JsonElement m, bool onboarding)
    {
        var t = m.GetProperty("t").GetString();
        try
        {
            switch (t)
            {
                case "ayar": AyarUygula(m.GetProperty("degerler")); break;
                case "kanca":
                    try { if (m.GetProperty("acik").GetBoolean()) Kancalar.Kur(); else Kancalar.Kaldir(); }
                    catch (Exception e) { kurulum?.Gonder(new { t = "hata", mesaj = e.Message }); }
                    kurulum?.DurumGonder();
                    break;
                case "baslangic": BaslangicKaydi.Ayarla(m.GetProperty("acik").GetBoolean()); TepsiMenusunuYenile(); kurulum?.DurumGonder(); break;
                case "klasorEkle": KlasorEkleDiyalog(); break;
                case "kisayollar": PaneliAc(true); break;
                case "guncelle": _ = GuncellemeDenetle(true); break;
                case "yenidenBaslat": Surum.UygulaVeYenidenBaslat(); break;
                case "klasorAc":
                    var neresi = m.GetProperty("ne").GetString() == "kayit" ? Kayit.Klasor : Ayarlar.Klasoru;
                    Directory.CreateDirectory(neresi);
                    Process.Start("explorer.exe", $"\"{neresi}\"");
                    break;
                case "sorun":
                    var z = Kayit.Paketle();
                    if (z != null) Process.Start("explorer.exe", $"/select,\"{z}\"");
                    break;
                case "bitir":
                    ayar.OnboardingBitti = true; ayar.Kaydet();
                    bool ilkClaude = m.TryGetProperty("ilkClaude", out var ic) && ic.GetBoolean();
                    kurulum?.Close();
                    Basla();
                    if (ilkClaude) YeniClaude(ayar.VarsayilanKlasor);
                    else tepsi.ShowBalloonTip(5000, "Clauding", Metin.T("calisiyor", Okunur("yeni"), Ayarlar.Mod), ToolTipIcon.Info);
                    break;
                case "kapat": kurulum?.Close(); break;
                case "kucult": if (kurulum != null) kurulum.WindowState = FormWindowState.Minimized; break;
            }
        }
        catch (Exception e) { Kayit.Yaz($"settings message '{t}' failed: {e.Message}"); }
    }

    /// <summary>Settings change one by one from the page and apply right away.</summary>
    void AyarUygula(JsonElement d)
    {
        bool diz = false, kisayol = false, menu = false;
        foreach (var p in d.EnumerateObject())
        {
            var v = p.Value;
            switch (p.Name)
            {
                case "dil": ayar.Dil = v.GetString() ?? "auto"; Metin.Dil = ayar.EtkinDil; menu = true; break;
                case "kabuk": ayar.Kabuk = v.GetString() ?? "powershell"; break;
                case "terminal": ayar.Terminal = v.GetString() ?? "wt"; break;
                case "odakModu": ayar.OdakModu = v.GetBoolean(); break;
                case "klasorler":
                    var liste = v.EnumerateArray().Select(x => new Klasor { Yol = x.GetProperty("yol").GetString() ?? "" }).Where(k => k.Yol != "").ToList();
                    if (liste.Count > 0) { ayar.Klasorler = liste; ayar.VarsayilanKlasor = Math.Clamp(ayar.VarsayilanKlasor, 0, liste.Count - 1); menu = true; }
                    break;
                case "varsayilan": ayar.VarsayilanKlasor = Math.Clamp(v.GetInt32(), 0, ayar.Klasorler.Count - 1); menu = true; break;
                case "klasorSor": ayar.KlasorSor = v.GetBoolean(); break;
                case "dizilim": ayar.Dizilim = v.GetString() ?? "auto"; diz = true; break;
                case "gelismis": ayar.Gelismis = v.GetBoolean(); diz = true; break;
                case "minGenislik": ayar.MinGenislik = Math.Clamp(v.GetInt32(), 300, 4000); diz = true; break;
                case "satirMaks": ayar.SatirMaks = Math.Clamp(v.GetInt32(), 1, 8); diz = true; break;
                case "anaOran": ayar.AnaOran = Math.Clamp(v.GetDouble(), 0.3, 0.85); ayar.Duzenler.Clear(); diz = true; break;
                case "bosluk": ayar.Bosluk = Math.Clamp(v.GetInt32(), 0, 60); diz = true; break;
                case "dikeyDizilim": ayar.DikeyDizilim = v.GetString() ?? "rows"; diz = true; break;
                case "tasma": ayar.AnaEkranKapasite = Math.Clamp(v.GetInt32(), 1, 20); diz = true; break;
                case "ikinciEkran": ayar.IkinciEkraniKullan = v.GetBoolean(); diz = true; break;
                case "tema": ayar.Tema = v.GetString() ?? "charcoal"; break;
                case "vurgu": ayar.Vurgu = v.GetString() ?? "#ff6a38"; break;
                case "efekt": ayar.Efekt = v.GetBoolean(); break;
                case "animasyonMs": ayar.AnimasyonMs = Math.Clamp(v.GetInt32(), 0, 600); break;
                case "guncelle": ayar.GuncellemeDenetle = v.GetBoolean(); break;
                case "tekGorevDugmesi": ayar.TekGorevDugmesi = v.GetBoolean(); diz = true; break;
                case "makroKlavye": ayar.MakroKlavye = v.GetBoolean(); kisayol = true; break;
                case "kapanisMetni": ayar.KapanisMetni = v.GetString() ?? ""; break;
                case "rehberBekleme": ayar.RehberBekleme = Math.Clamp(v.GetInt32(), 0, 3000); break;
                case "mod":
                {
                    // move every shortcut that uses the old modifier combo onto the new one
                    var yeniMod = v.GetString() ?? Ayarlar.Mod;
                    var y = ayar.Klavye.GetValueOrDefault("yeni", Ayarlar.Mod + "+N");
                    var eskiMod = y.Contains('+') ? y[..y.LastIndexOf('+')] : Ayarlar.Mod;
                    if (eskiMod == yeniMod) break;
                    foreach (var k in ayar.Klavye.Keys.ToList())
                        if (ayar.Klavye[k].StartsWith(eskiMod + "+", StringComparison.OrdinalIgnoreCase))
                        {
                            var tus = ayar.Klavye[k][(eskiMod.Length + 1)..];
                            // Ctrl+Win+arrows switch virtual desktops: leave those empty under Ctrl+Win
                            ayar.Klavye[k] = yeniMod == "Ctrl+Win" && tus is "Left" or "Right" ? "" : yeniMod + "+" + tus;
                        }
                    kisayol = true; menu = true;
                    break;
                }
            }
        }
        ayar.Kaydet();
        if (kisayol && basladi) KisayollariKaydet();
        if (menu) { TepsiMenusunuYenile(); TepsiYazisi(); }
        if (diz) DizIste(true); else SahneIste();
    }
}
