using System.Diagnostics;

namespace Clauding;

/// <summary>Commands (keys, tray, scene buttons), global hotkeys, the hold-to-see-shortcuts guide and the tray.</summary>
sealed partial class Yonetici
{
    readonly List<string> kisayolHatalari = new();
    readonly Dictionary<int, Action> kisayolEylem = new();
    ToolStripMenuItem? duraklatMenu, baslangicMenu, guncelleMenu;

    // ---------- which Claude a command acts on ----------

    IntPtr Aktif()
    {
        var h = N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT);
        if (Uygun(h) && !sira.Contains(h)) Ekle(h);
        return sira.Contains(h) ? h : Yonetilen(sonOdak) ? sonOdak : IntPtr.Zero;
    }

    IntPtr Secili()
    {
        var h = N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT);
        return Yonetilen(h) ? h : Yonetilen(odak) ? odak : Yonetilen(sonOdak) ? sonOdak : IntPtr.Zero;
    }

    void Odakla(IntPtr h)
    {
        if (!Yonetilen(h)) return;
        if (N.IsIconic(h)) N.ShowWindow(h, N.SW_RESTORE);
        N.SetForegroundWindow(h);
    }

    // ---------- new Claude, folders ----------

    public void YeniClaude()
    {
        if (ayar.KlasorSor && ayar.Klasorler.Count > 1) { KlasorSeciciAc(); return; }
        YeniClaude(ayar.VarsayilanKlasor);
    }

    public void YeniClaude(int i)
    {
        if (i < 0 || i >= ayar.Klasorler.Count) { System.Media.SystemSounds.Beep.Play(); return; }
        if (!Terminal.Ac(ayar, ayar.Klasorler[i].Acik, out var hata))
            tepsi.ShowBalloonTip(5000, "Clauding", Metin.T("terminalYok", hata), ToolTipIcon.Error);
    }

    KlasorSecici? secici;

    void KlasorSeciciAc() => KlasorSeciciAc(null);

    /// <summary>The folder picker: centred near the top of the screen (shortcut) or under the scene's ▾ button.</summary>
    void KlasorSeciciAc(Point? altKose)
    {
        secici?.Close();
        secici = new KlasorSecici(ayar.Klasorler.Select(k => (k.Gorunen, k.Yol)).ToList(), ayar.VarsayilanKlasor);
        secici.Secildi += i => { if (i == -2) KlasorEkleDiyalog(); else YeniClaude(i); };
        var ekran = Screen.FromPoint(altKose ?? Cursor.Position);
        secici.Goster(ekran.WorkingArea, altKose);
    }

    void KlasorEkleDiyalog()
    {
        using var d = new FolderBrowserDialog { Description = Metin.T("klasorSecBaslik"), UseDescriptionForTitle = true, ShowNewFolderButton = true };
        if (d.ShowDialog() != DialogResult.OK || string.IsNullOrWhiteSpace(d.SelectedPath)) return;
        if (ayar.Klasorler.Any(k => string.Equals(k.Acik.TrimEnd('\\'), d.SelectedPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))) return;
        ayar.Klasorler.Add(new Klasor { Yol = d.SelectedPath });
        ayar.Kaydet();
        SahneIste();
        TepsiMenusunuYenile();
        kurulum?.DurumGonder();
    }

    // ---------- window commands ----------

    void AyirTak() { var h = Aktif(); if (h != IntPtr.Zero) AyirTak(h); }

    void AyirTak(IntPtr h)
    {
        if (ayrik.Remove(h)) { DizIste(true); return; }
        ayrik.Add(h);
        if (N.IsZoomed(h)) N.ShowWindow(h, N.SW_RESTORE);
        var a = AnaEkran().WorkingArea;
        var r = new Rectangle(a.X + a.Width / 5, a.Y + a.Height / 6, a.Width * 3 / 5, a.Height * 2 / 3);
        var (l, t, rr, b) = N.Kenarlar(h);
        N.SetWindowPos(h, IntPtr.Zero, r.X - l, r.Y - t, r.Width + l + rr, r.Height + t + b, N.SWP_NOOWNERZORDER);
        N.SetForegroundWindow(h);
        DizIste();
    }

    void HepsiniGeriTak() { ayrik.Clear(); Diz(true); }

    void Buyut()
    {
        var h = Aktif();
        if (h == IntPtr.Zero) return;
        N.ShowWindow(h, N.IsZoomed(h) ? N.SW_RESTORE : N.SW_MAXIMIZE);
        DizIste();
    }

    void Kaydir(int yon)
    {
        var h = Aktif();
        if (h == IntPtr.Zero || ayrik.Contains(h)) return;
        int i = sira.IndexOf(h);
        for (int j = i + yon; j >= 0 && j < sira.Count; j += yon)
        {
            if (ayrik.Contains(sira[j]) || N.IsIconic(sira[j]) || N.IsZoomed(sira[j])) continue;
            (sira[i], sira[j]) = (sira[j], sira[i]);
            break;
        }
        DizIste(true);
    }

    /// <summary>Tiles in reading order: main screen first (top to bottom, left to right), then the second.</summary>
    List<IntPtr> OkumaSirasi() => karolar
        .OrderBy(kv => kv.Value.Sahne == sahneAna ? 0 : 1)
        .ThenBy(kv => kv.Value.Hucre.Top / 40).ThenBy(kv => kv.Value.Hucre.Left)
        .Select(kv => kv.Key).ToList();

    void Git(int n)
    {
        var liste = OkumaSirasi();
        if (n < 1 || n > liste.Count) { System.Media.SystemSounds.Beep.Play(); return; }
        Odakla(liste[n - 1]);
    }

    /// <summary>Focus the nearest tile in a direction (dx, dy), measured between tile centres.</summary>
    void OdakYon(int dx, int dy)
    {
        var h = Aktif();
        if (h == IntPtr.Zero || !karolar.TryGetValue(h, out var k)) { var ilk = OkumaSirasi().FirstOrDefault(); if (ilk != IntPtr.Zero) Odakla(ilk); return; }
        var m = new Point(k.Hucre.Left + k.Hucre.Width / 2, k.Hucre.Top + k.Hucre.Height / 2);
        IntPtr en = IntPtr.Zero; double enIyi = double.MaxValue;
        foreach (var (o, ok) in karolar)
        {
            if (o == h) continue;
            var c = new Point(ok.Hucre.Left + ok.Hucre.Width / 2, ok.Hucre.Top + ok.Hucre.Height / 2);
            int ileri = (c.X - m.X) * dx + (c.Y - m.Y) * dy;           // distance along the direction
            int yan = Math.Abs(dx != 0 ? c.Y - m.Y : c.X - m.X);        // sideways drift
            if (ileri <= 0) continue;
            double puan = ileri + yan * 2.0;
            if (puan < enIyi) { enIyi = puan; en = o; }
        }
        if (en != IntPtr.Zero) Odakla(en); else System.Media.SystemSounds.Beep.Play();
    }

    void BekleyeneGit()
    {
        var h = isaret.Where(kv => N.IsWindow(kv.Key)).OrderBy(kv => kv.Value.durum == "izin" ? 0 : 1).ThenBy(kv => kv.Value.zaman).Select(kv => kv.Key).FirstOrDefault();
        if (h == IntPtr.Zero) { System.Media.SystemSounds.Beep.Play(); return; }
        Odakla(h);
    }

    /// <summary>Jump to the Claude that asks for permission and press Enter (= the highlighted "Yes").</summary>
    void IzinVer()
    {
        var h = isaret.Where(kv => kv.Value.durum == "izin" && N.IsWindow(kv.Key)).OrderBy(kv => kv.Value.zaman).Select(kv => kv.Key).FirstOrDefault();
        if (h == IntPtr.Zero) { System.Media.SystemSounds.Beep.Play(); return; }
        Odakla(h);
        Thread.Sleep(80);
        if (N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT) == h) N.TusBas(Keys.Enter);
    }

    void Durdur()
    {
        var h = N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT);
        if (Uygun(h)) N.TusBas(Keys.Escape);
    }

    void AlanDongu() => AlanAyarla(AlanAdi() switch { "tam" => "sol", "sol" => "sag", _ => "tam" });

    void GizleGoster()
    {
        bool acikVar = sira.Any(h => N.IsWindow(h) && !N.IsIconic(h));
        foreach (var h in sira.ToList()) N.ShowWindow(h, acikVar ? 6 /*SW_MINIMIZE*/ : N.SW_RESTORE);
        DizIste();
    }

    void Duraklat()
    {
        duraklat = !duraklat;
        if (duraklatMenu != null) duraklatMenu.Checked = duraklat;
        Diz();
    }

    // ---------- close (optionally: type a closing line first, close after Claude answers) ----------

    readonly Dictionary<IntPtr, (string durum, DateTime zaman)> kapanis = new(); // kaydediliyor | hazir
    readonly HashSet<IntPtr> titret = new();
    readonly System.Windows.Forms.Timer kapanisZ = new() { Interval = 40 };
    readonly Dictionary<IntPtr, DateTime> sonBasis = new();
    IntPtr ilkBasis; DateTime ilkBasisZamani;
    bool kapanisBagli;

    /// <summary>
    /// Without a closing line: one press closes. With one: the first press types it (after a short wait in
    /// case a second press comes), the pill says "close now" when Claude has answered, the next press closes.
    /// Two presses within a second always close right away.
    /// </summary>
    void KapatBas(IntPtr h)
    {
        if (h == IntPtr.Zero) { System.Media.SystemSounds.Beep.Play(); return; }
        if (string.IsNullOrWhiteSpace(ayar.KapanisMetni)) { Kapat(h); return; }
        if (!kapanisBagli) { kapanisZ.Tick += (_, _) => KapanisAdim(); kapanisBagli = true; }
        var simdi = DateTime.Now;
        bool cift = sonBasis.TryGetValue(h, out var onceki) && (simdi - onceki).TotalMilliseconds < 1000;
        sonBasis[h] = simdi;
        if (cift) { if (ilkBasis == h) ilkBasis = IntPtr.Zero; sonBasis.Remove(h); Kapat(h); return; }

        kapanis.TryGetValue(h, out var st);
        if (st.durum == "hazir") { Kapat(h); return; }
        if (st.durum == "kaydediliyor") { titret.Add(h); SahneIste(); return; }
        ilkBasis = h; ilkBasisZamani = simdi;
        kapanisZ.Start();
    }

    void KapanisAdim()
    {
        if (ilkBasis != IntPtr.Zero && (DateTime.Now - ilkBasisZamani).TotalMilliseconds >= 380)
        {
            var h = ilkBasis; ilkBasis = IntPtr.Zero;
            if (Yonetilen(h) && !kapanis.ContainsKey(h)) KapanisYaz(h);
        }
        if (ilkBasis == IntPtr.Zero) kapanisZ.Stop();
    }

    void KapanisYaz(IntPtr h)
    {
        Odakla(h);
        Thread.Sleep(90);
        if (N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT) != h) { System.Media.SystemSounds.Beep.Play(); return; }
        N.YaziYaz(ayar.KapanisMetni);
        Thread.Sleep(40);
        N.TusBas(Keys.Enter);
        kapanis[h] = ("kaydediliyor", DateTime.Now);
        SahneIste();
    }

    void Kapat(IntPtr h)
    {
        kapanis.Remove(h); sonBasis.Remove(h);
        N.PostMessage(h, N.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        SahneIste();
    }

    // ---------- floating "back to the grid" button on maximised terminals, handles on detached ones ----------

    readonly Dictionary<IntPtr, KucultDugmesi> kucultler = new();

    void KucultGuncelle(IntPtr h, bool buyukMu)
    {
        if (!buyukMu || !N.IsWindow(h))
        {
            if (kucultler.Remove(h, out var eskiD)) { eskiD.Close(); eskiD.Dispose(); }
            return;
        }
        if (!kucultler.TryGetValue(h, out var d))
        {
            d = new KucultDugmesi(h);
            d.Tiklandi += () => { N.ShowWindow(h, N.SW_RESTORE); N.SetForegroundWindow(h); DizIste(true); };
            kucultler[h] = d;
        }
        d.Yerles();
    }

    readonly Dictionary<IntPtr, Tutamak> tutamaklar = new();

    void TutamakGuncelle(IntPtr h)
    {
        if (!tutamaklar.TryGetValue(h, out var t))
        {
            t = new Tutamak(h);
            t.GeriTak += () => { ayrik.Remove(h); DizIste(true); };
            t.Buyut += () => { N.ShowWindow(h, N.IsZoomed(h) ? N.SW_RESTORE : N.SW_MAXIMIZE); N.SetForegroundWindow(h); };
            tutamaklar[h] = t;
        }
        t.Baslik(basliklar.GetValueOrDefault(h, "Claude"));
        t.Yerles();
    }

    void TutamakSil(IntPtr h)
    {
        if (tutamaklar.Remove(h, out var t)) { t.Close(); t.Dispose(); }
    }

    // ---------- actions, hotkeys ----------

    Dictionary<string, Action> Eylemler()
    {
        var e = new Dictionary<string, Action>
        {
            ["yeni"] = YeniClaude, ["klasorSec"] = KlasorSeciciAc,
            ["bekleyen"] = BekleyeneGit, ["izin"] = IzinVer, ["durdur"] = Durdur, ["kapat"] = () => KapatBas(Secili()),
            ["odakSol"] = () => OdakYon(-1, 0), ["odakSag"] = () => OdakYon(1, 0), ["odakYukari"] = () => OdakYon(0, -1), ["odakAsagi"] = () => OdakYon(0, 1),
            ["sola"] = () => Kaydir(-1), ["saga"] = () => Kaydir(1),
            ["buyut"] = Buyut, ["ayir"] = AyirTak, ["diz"] = HepsiniGeriTak, ["gizle"] = GizleGoster,
            ["alanTam"] = () => AlanAyarla("tam"), ["alanSol"] = () => AlanAyarla("sol"), ["alanSag"] = () => AlanAyarla("sag"),
            ["alanSol23"] = () => AlanAyarla("sol23"), ["alanSag23"] = () => AlanAyarla("sag23"), ["alan"] = AlanDongu, ["ekran"] = EkranDegistir,
            ["dizilim"] = SonrakiDizilim, ["dizilimAuto"] = () => DizilimAyarla("auto"), ["dizilimCols"] = () => DizilimAyarla("cols"),
            ["dizilimMain"] = () => DizilimAyarla("main"), ["dizilimRows"] = () => DizilimAyarla("rows"),
            ["duraklat"] = Duraklat, ["rehber"] = () => PaneliAc(false),
        };
        for (int i = 1; i <= 9; i++) { int n = i; e["git" + i] = () => Git(n); e["yeni" + i] = () => YeniClaude(n - 1); }
        return e;
    }

    /// <summary>Keyboard shortcuts plus (when a macro pad is on) F13–F24. Safe to call again after edits.</summary>
    void KisayollariKaydet()
    {
        KisayollariBirak();
        kisayolHatalari.Clear();
        var eylem = Eylemler();
        int id = 1;
        void Kaydet(string tus, string ad)
        {
            if (string.IsNullOrWhiteSpace(tus) || !eylem.TryGetValue(ad, out var e)) return;
            if (Ayarlar.KisayolCoz(tus, out var mods, out var vk) && N.RegisterHotKey(msj.Handle, id, mods, vk)) kisayolEylem[id] = e;
            else kisayolHatalari.Add(KisayolGoster(tus));
            id++;
        }
        if (ayar.MakroKlavye)
            for (int i = 0; i < 12 && i < ayar.Makro.Count; i++) Kaydet("F" + (13 + i), ayar.Makro[i]);
        foreach (var (ad, tus) in ayar.Klavye) Kaydet(tus, ad);
    }

    void KisayollariBirak()
    {
        foreach (var eskiId in kisayolEylem.Keys) N.UnregisterHotKey(msj.Handle, eskiId);
        kisayolEylem.Clear();
    }

    /// <summary>Is this combo free (not taken by another app)? Tries to register it for a moment.</summary>
    bool KisayolBos(string tus)
    {
        if (!Ayarlar.KisayolCoz(tus, out var mods, out var vk)) return false;
        const int deneme = 0xBFFF;
        if (!N.RegisterHotKey(msj.Handle, deneme, mods, vk)) return false;
        N.UnregisterHotKey(msj.Handle, deneme);
        return true;
    }

    public void KisayolGeldi(int id)
    {
        rehberKisayolKullanildi = true;
        if (kisayolEylem.TryGetValue(id, out var e))
        {
            try { e(); } catch (Exception ex) { Kayit.Yaz("action failed: " + ex.Message); }
        }
    }

    // ---------- hold the modifier combo to see every shortcut ----------

    readonly System.Windows.Forms.Timer rehberZ = new() { Interval = 80 };
    DateTime? rehberBasladi;
    bool rehberAcik, rehberKisayolKullanildi;

    void RehberIzlemeyiKur()
    {
        rehberZ.Tick += (_, _) => RehberAdim();
        rehberZ.Start();
    }

    /// <summary>The modifiers of the "New Claude" shortcut (default Ctrl+Win+Alt).</summary>
    (bool ctrl, bool win, bool alt, bool shift) RehberModu()
    {
        var p = (ayar.Klavye.GetValueOrDefault("yeni", Ayarlar.Mod + "+N")).Split('+').Select(x => x.ToLowerInvariant()).ToHashSet();
        return (p.Contains("ctrl"), p.Contains("win"), p.Contains("alt"), p.Contains("shift"));
    }

    void RehberAdim()
    {
        if (ayar.RehberBekleme <= 0 || kayitModu) return;
        var (c, w, a, s) = RehberModu();
        if (!c && !w && !a) return;
        bool ctrl = N.Basili(Keys.ControlKey), win = N.Basili(Keys.LWin) || N.Basili(Keys.RWin), alt = N.Basili(Keys.Menu), shift = N.Basili(Keys.ShiftKey);
        bool tutuluyor = ctrl == c && win == w && alt == a && shift == s;
        if (tutuluyor)
        {
            if (rehberBasladi == null) { rehberBasladi = DateTime.Now; rehberKisayolKullanildi = false; }
            else if (!rehberAcik && !rehberKisayolKullanildi && (DateTime.Now - rehberBasladi.Value).TotalMilliseconds >= ayar.RehberBekleme)
            {
                rehberAcik = true;
                PaneliAc(false, rehber: true);
            }
        }
        else
        {
            rehberBasladi = null;
            if (rehberAcik) { rehberAcik = false; panel?.RehberKapat(); }
        }
    }

    // ---------- tray ----------

    void TepsiKur()
    {
        try { tepsi.Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "icon.ico"), SystemInformation.SmallIconSize); }
        catch { tepsi.Icon = SimgeCiz(); }
        TepsiMenusunuYenile();
        tepsi.DoubleClick += (_, _) => { if (basladi) HepsiniGeriTak(); else KurulumAc(true); };
        tepsi.Visible = true;
        TepsiYazisi();
    }

    void TepsiMenusunuYenile()
    {
        var m = new ContextMenuStrip();
        string K(string e) => Okunur(e) is { Length: > 0 } s ? $"\t{s}" : "";
        if (ayar.Klasorler.Count > 1)
        {
            var yeni = new ToolStripMenuItem(Metin.T("yeni"));
            for (int i = 0; i < ayar.Klasorler.Count; i++) { int n = i; yeni.DropDownItems.Add($"{ayar.Klasorler[i].Gorunen}{(i == ayar.VarsayilanKlasor ? "  ★" : "")}", null, (_, _) => YeniClaude(n)); }
            yeni.DropDownItems.Add(new ToolStripSeparator());
            yeni.DropDownItems.Add(Metin.T("klasorEkle"), null, (_, _) => KlasorEkleDiyalog());
            m.Items.Add(yeni);
        }
        else m.Items.Add(Metin.T("yeni") + K("yeni"), null, (_, _) => YeniClaude());
        m.Items.Add(Metin.T("diz") + K("diz"), null, (_, _) => HepsiniGeriTak());
        m.Items.Add(Metin.T("bekleyen") + K("bekleyen"), null, (_, _) => BekleyeneGit());
        duraklatMenu = new ToolStripMenuItem(Metin.T("duraklat") + K("duraklat"), null, (_, _) => Duraklat()) { Checked = duraklat };
        m.Items.Add(duraklatMenu);
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add(Metin.T("kisayollar") + K("rehber"), null, (_, _) => PaneliAc(true));
        m.Items.Add(Metin.T("ayarlar"), null, (_, _) => AyarlariAc());
        baslangicMenu = new ToolStripMenuItem(Metin.T("baslangic"), null, (_, _) => { BaslangicKaydi.Ayarla(!BaslangicKaydi.Acik); baslangicMenu!.Checked = BaslangicKaydi.Acik; }) { Checked = BaslangicKaydi.Acik };
        m.Items.Add(baslangicMenu);
        guncelleMenu = new ToolStripMenuItem(Surum.HazirSurum != null ? Metin.T("yenidenBaslat") : Metin.T("guncelle"), null, (_, _) =>
        {
            if (Surum.HazirSurum != null) Surum.UygulaVeYenidenBaslat(); else _ = GuncellemeDenetle(true);
        });
        m.Items.Add(guncelleMenu);
        m.Items.Add(Metin.T("sorun"), null, (_, _) =>
        {
            var z = Kayit.Paketle();
            if (z != null) { tepsi.ShowBalloonTip(5000, "Clauding", Metin.T("sorunHazir", Path.GetFileName(z)), ToolTipIcon.Info); Process.Start("explorer.exe", $"/select,\"{z}\""); }
        });
        m.Items.Add(new ToolStripSeparator());
        m.Items.Add($"Clauding {Surum.Simdiki}", null, (_, _) => Process.Start(new ProcessStartInfo(Surum.Depo) { UseShellExecute = true }));
        m.Items.Add(Metin.T("cikis"), null, (_, _) => ExitThread());
        var eski = tepsi.ContextMenuStrip;
        tepsi.ContextMenuStrip = m;
        eski?.Dispose();
    }

    void TepsiYazisi()
    {
        int izin = isaret.Count(kv => kv.Value.durum == "izin"), bitti = isaret.Count - izin;
        var p = new List<string> { Metin.T("pencere", sira.Count) };
        if (ayrik.Count > 0) p.Add(Metin.T("ayrik", ayrik.Count));
        if (izin > 0) p.Add(Metin.T("izinBek", izin));
        if (bitti > 0) p.Add(Metin.T("bitti", bitti));
        var s = "Clauding: " + string.Join(", ", p) + (duraklat ? $" ({Metin.T("duraklatildi")})" : "");
        tepsi.Text = s.Length > 127 ? s[..127] : s;
    }

    async Task GuncellemeDenetle(bool elle)
    {
        if (!elle && !ayar.GuncellemeDenetle) return;
        try
        {
            var yeni = await Surum.DenetleVeIndir();
            if (yeni != null)
            {
                tepsi.ShowBalloonTip(6000, "Clauding", Metin.T("guncelVar", yeni), ToolTipIcon.Info);
                TepsiMenusunuYenile();
            }
            else if (elle) tepsi.ShowBalloonTip(3000, "Clauding", Surum.Kurulu ? Metin.T("guncelYok") : Metin.T("guncelYok") + " (dev build)", ToolTipIcon.Info);
        }
        catch { if (elle) tepsi.ShowBalloonTip(4000, "Clauding", Metin.T("guncelHata"), ToolTipIcon.Warning); }
    }

    static Icon SimgeCiz()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            using var b1 = new SolidBrush(Color.FromArgb(236, 232, 225));
            using var b2 = new SolidBrush(Color.FromArgb(255, 106, 56));
            g.FillRectangle(b1, 2, 2, 13, 13); g.FillRectangle(b1, 17, 2, 13, 13);
            g.FillRectangle(b1, 2, 17, 13, 13); g.FillEllipse(b2, 18, 18, 11, 11);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }
}
