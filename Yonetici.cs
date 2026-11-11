using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace Clauding;

/// <summary>
/// Tray app: tiles every Claude terminal window (Windows Terminal, or classic consoles it started) inside a
/// work area, draws the scene behind them (Sahne), keeps the group together in z-order, and shows which
/// Claude is working, waiting for you or done. Split over several files: layout (Yonetici.Dizilim.cs),
/// commands and keys (Yonetici.Eylemler.cs), panels and windows (Yonetici.Pencereler.cs).
/// </summary>
sealed partial class Yonetici : ApplicationContext
{
    readonly Ayarlar ayar = Ayarlar.Yukle();
    readonly NotifyIcon tepsi = new();
    readonly MesajPenceresi msj;
    readonly System.Windows.Forms.Timer dizZamanlayici = new() { Interval = 90 };
    readonly System.Windows.Forms.Timer sahneZamanlayici = new() { Interval = 150 };
    readonly N.WinEventDelegate olayCb;
    readonly List<IntPtr> kancalar = new();
    // a click anywhere in the app (any tile or the scene) brings the whole group forward, even when the
    // clicked terminal was already the foreground window (no FOREGROUND event then)
    readonly N.LowLevelProc fareCb;
    IntPtr fareKanca, tiklanan;
    readonly System.Windows.Forms.Timer oneAlZ = new() { Interval = 60 };

    readonly List<IntPtr> sira = new();                  // tiled order
    readonly HashSet<IntPtr> ayrik = new();              // pulled out (floating)
    readonly HashSet<IntPtr> konsollar = new();          // classic console windows we manage
    readonly Dictionary<IntPtr, bool> buyuk = new();     // last known maximized state
    readonly Dictionary<IntPtr, Karo> karolar = new();   // where each tiled window is
    readonly Dictionary<IntPtr, (string durum, DateTime zaman)> isaret = new(); // izin | bitti (from hooks)
    readonly Dictionary<IntPtr, (bool calisiyor, DateTime zaman)> faaliyet = new(); // from the title spinner
    readonly Dictionary<IntPtr, string> basliklar = new();
    readonly Sahne sahneAna, sahneIkinci;
    readonly GorevDugmesi gorevDugmesi = new();
    readonly HashSet<IntPtr> gorevdenGizli = new(); // terminals we took off the taskbar
    bool grupOndeydi, grupKucuk; // a tiled Claude was the last foreground window / the group was minimized from its button
    IntPtr suruklenen, odak, sonOdak; // sonOdak: the last Claude you were in, kept while another app is in front
    Rectangle surukBas;
    bool diziliyor, duraklat, basladi;
    int anaSayi;
    Plan anaPlan = new(false, Array.Empty<int>(), "auto");

    record Karo(Sahne Sahne, Rectangle Hucre, Rectangle Terminal, int Satir, int Sutun, int SutunSayisi, int SatirSayisi);

    public Yonetici()
    {
        Metin.Dil = ayar.EtkinDil;
        msj = new MesajPenceresi(this);
        sahneAna = new Sahne(false);
        sahneIkinci = new Sahne(true);
        foreach (var s in new[] { sahneAna, sahneIkinci })
        {
            var sah = s;
            s.Mesaj += m => SahneMesaji(sah, m);
            s.Tiklandi += () => { if (Yonetilen(odak)) N.SetForegroundWindow(odak); };
        }
        dizZamanlayici.Tick += (_, _) => { dizZamanlayici.Stop(); var a = animIste; animIste = false; Diz(a); };
        animZamanlayici.Tick += (_, _) => AnimAdim();
        sahneZamanlayici.Tick += (_, _) => { sahneZamanlayici.Stop(); SahneyiGuncelle(); };
        olayCb = Olay;
        fareCb = FareKanca;
        oneAlZ.Tick += (_, _) => { oneAlZ.Stop(); TiklamaSonrasi(); };
        gorevDugmesi.Tiklandi += GorevTiklandi;
        gorevDugmesi.Onizleme = Onizleme;
        gorevDugmesi.TamOnizleme = TamOnizleme;
        onizlemeZ.Tick += (_, _) => { if (GrupOnde()) { OnizlemeYakala(); gorevDugmesi.OnizlemeEskidi(); } };
        gorevZ.Tick += (_, _) => { gorevZ.Stop(); GorevSenkron(); };

        TepsiKur();
        if (!ayar.OnboardingBitti) KurulumAc(true);
        else Basla();
    }

    /// <summary>Everything that watches and moves windows. Runs after the onboarding (or right away).</summary>
    void Basla()
    {
        if (basladi) return;
        basladi = true;
        void Kanca(uint a, uint b) => kancalar.Add(N.SetWinEventHook(a, b, IntPtr.Zero, olayCb, 0, 0, N.WINEVENT_OUTOFCONTEXT | N.WINEVENT_SKIPOWNPROCESS));
        Kanca(N.EVENT_SYSTEM_FOREGROUND, N.EVENT_SYSTEM_FOREGROUND);
        Kanca(N.EVENT_SYSTEM_MOVESIZESTART, N.EVENT_SYSTEM_MOVESIZEEND);
        Kanca(N.EVENT_SYSTEM_MINIMIZESTART, N.EVENT_SYSTEM_MINIMIZEEND);
        Kanca(N.EVENT_OBJECT_DESTROY, N.EVENT_OBJECT_HIDE);
        Kanca(N.EVENT_OBJECT_LOCATIONCHANGE, N.EVENT_OBJECT_NAMECHANGE);
        fareKanca = N.SetWindowsHookEx(N.WH_MOUSE_LL, fareCb, N.GetModuleHandle(null), 0);

        KisayollariKaydet();
        RehberIzlemeyiKur();
        MevcutlariTopla();
        odak = N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT);
        if (Yonetilen(odak)) sonOdak = odak; // no FOREGROUND event comes for the window you are already in
        Diz();
        grupOndeydi = karolar.ContainsKey(odak);
        onizlemeZ.Start();
        foreach (var h in sira.Where(N.IsZoomed).ToList()) KucultGuncelle(h, true);
        SystemEvents.DisplaySettingsChanged += (_, _) => { DizIste(); SahneIste(); };
        Kayit.Yaz($"tiling started: {sira.Count} windows, {kisayolEylem.Count} shortcuts, taken: {(kisayolHatalari.Count == 0 ? "none" : string.Join(", ", kisayolHatalari))}");
        if (kisayolHatalari.Count > 0)
            tepsi.ShowBalloonTip(6000, "Clauding", Metin.T("kisayolDolu", string.Join(", ", kisayolHatalari)), ToolTipIcon.Warning);
        _ = GuncellemeDenetle(false);
    }

    // ---------- windows ----------

    bool Uygun(IntPtr h)
    {
        if (h == IntPtr.Zero || !N.IsWindowVisible(h) || N.GetWindow(h, N.GW_OWNER) != IntPtr.Zero) return false;
        var s = N.Sinif(h);
        return s == N.TerminalSinifi || (s == N.KonsolSinifi && konsollar.Contains(h));
    }

    bool Yonetilen(IntPtr h) => h != IntPtr.Zero && sira.Contains(h) && N.IsWindow(h);

    /// <summary>A console title Claude Code wrote: a spinner glyph (or ✳) + space + text.</summary>
    static bool ClaudeBasligi(string t) => t.Length > 2 && t[1] == ' ' && !char.IsLetterOrDigit(t[0]);

    void MevcutlariTopla()
    {
        var bulunan = new List<IntPtr>();
        N.EnumWindows((h, _) =>
        {
            if (N.Sinif(h) == N.KonsolSinifi && N.IsWindowVisible(h) && ClaudeBasligi(N.Baslik(h).Trim())) konsollar.Add(h);
            if (Uygun(h)) bulunan.Add(h);
            return true;
        }, IntPtr.Zero);
        var ana = Screen.PrimaryScreen!.Bounds;
        foreach (var h in bulunan.OrderBy(h => ana.Contains(N.GorunenCerceve(h).Location) ? 0 : 1)
                                 .ThenBy(h => N.GorunenCerceve(h).Y / 200).ThenBy(h => N.GorunenCerceve(h).X))
        {
            sira.Add(h); buyuk[h] = N.IsZoomed(h); BaslikOku(h);
        }
    }

    void Ekle(IntPtr h)
    {
        if (sira.Contains(h)) return;
        sira.Add(h); buyuk[h] = N.IsZoomed(h); BaslikOku(h);
        yeniler.Add(h);
        DizIste(true);
    }

    void Cikar(IntPtr h)
    {
        // the Claude you were in is closing: Windows would hand focus to whatever app is below the group
        // (the scene never takes focus), and that app would cover everything. Keep a neighbour in front instead.
        var komsu = (h == odak || h == sonOdak || N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT) == h) ? Komsu(h) : IntPtr.Zero;
        if (gorevdenGizli.Remove(h) && N.IsWindowVisible(h)) Gorev.Goster(h);
        bool vardi = sira.Remove(h);
        ayrik.Remove(h); buyuk.Remove(h); karolar.Remove(h); basliklar.Remove(h); faaliyet.Remove(h); konsollar.Remove(h);
        IsaretSil(h); TutamakSil(h); KucultGuncelle(h, false); yeniler.Remove(h); animler.Remove(h); kapanis.Remove(h);
        if (vardi) DizIste(true);
        if (komsu != IntPtr.Zero) { kapananKomsu = komsu; kapanisOdakZ.Stop(); kapanisOdakZ.Start(); }
    }

    IntPtr kapananKomsu;
    System.Windows.Forms.Timer? kapanisOdakZBase;
    System.Windows.Forms.Timer kapanisOdakZ
    {
        get
        {
            if (kapanisOdakZBase != null) return kapanisOdakZBase;
            kapanisOdakZBase = new System.Windows.Forms.Timer { Interval = 120 };
            kapanisOdakZBase.Tick += (_, _) =>
            {
                kapanisOdakZBase.Stop();
                var hedef = kapananKomsu; kapananKomsu = IntPtr.Zero;
                if (!Yonetilen(hedef)) hedef = karolar.Keys.FirstOrDefault(Yonetilen);
                if (hedef == IntPtr.Zero) return;
                var on = N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT);
                if (Yonetilen(on)) return; // focus already went to another Claude
                N.SetForegroundWindow(hedef);
                GrubuOneAl(hedef, true); // even if Windows refuses the focus change, the group comes back on top
            };
            return kapanisOdakZBase;
        }
    }

    /// <summary>The tile next to h in reading order (the one after it, or the one before if h was last).</summary>
    IntPtr Komsu(IntPtr h)
    {
        var liste = karolar.OrderBy(kv => kv.Value.Sahne == sahneAna ? 0 : 1).ThenBy(kv => kv.Value.Hucre.Top / 40).ThenBy(kv => kv.Value.Hucre.Left).Select(kv => kv.Key).ToList();
        int i = liste.IndexOf(h);
        if (i < 0) return liste.FirstOrDefault(x => x != h);
        return i + 1 < liste.Count ? liste[i + 1] : i > 0 ? liste[i - 1] : IntPtr.Zero;
    }

    bool animIste;
    void DizIste(bool anim = false) { if (!basladi) return; animIste |= anim; dizZamanlayici.Stop(); dizZamanlayici.Start(); }
    void SahneIste() { if (basladi && !sahneZamanlayici.Enabled) sahneZamanlayici.Start(); }

    Screen AnaEkran() => Screen.AllScreens.FirstOrDefault(s => s.DeviceName == ayar.Ekran) ?? Screen.PrimaryScreen!;

    Rectangle CalismaAlani()
    {
        var a = AnaEkran().WorkingArea;
        int x0 = a.X + (int)Math.Round(a.Width * ayar.AlanSol), x1 = a.X + (int)Math.Round(a.Width * ayar.AlanSag);
        int y0 = a.Y + (int)Math.Round(a.Height * ayar.AlanUst), y1 = a.Y + (int)Math.Round(a.Height * ayar.AlanAlt);
        return new Rectangle(x0, y0, Math.Max(400, x1 - x0), Math.Max(300, y1 - y0));
    }

    Rectangle? IkinciAlan()
    {
        if (!ayar.IkinciEkraniKullan) return null;
        var ana = AnaEkran();
        return Screen.AllScreens.FirstOrDefault(s => s.DeviceName != ana.DeviceName)?.WorkingArea;
    }

    /// <summary>Move the whole work area (scene + tiles) to the next monitor.</summary>
    void EkranDegistir()
    {
        var ekranlar = Screen.AllScreens;
        if (ekranlar.Length < 2) { System.Media.SystemSounds.Beep.Play(); return; }
        int i = Array.FindIndex(ekranlar, s => s.DeviceName == AnaEkran().DeviceName);
        ayar.Ekran = ekranlar[(i + 1) % ekranlar.Length].DeviceName;
        ayar.Kaydet();
        DizIste(true);
    }

    void Diz() => Diz(false);

    void Diz(bool anim)
    {
        if (!basladi) return;
        foreach (var h in sira.Where(h => !N.IsWindow(h) || !N.IsWindowVisible(h)).ToList()) Cikar(h);
        foreach (var h in tutamaklar.Keys.Where(h => !ayrik.Contains(h)).ToList()) TutamakSil(h);
        foreach (var h in ayrik) TutamakGuncelle(h);
        if (duraklat) { animZamanlayici.Stop(); sahneAna.Hide(); sahneIkinci.Hide(); GorevSenkron(); TepsiYazisi(); return; }

        var aktif = sira.Where(h => !ayrik.Contains(h) && !N.IsIconic(h) && !N.IsZoomed(h)).ToList();
        var ikinci = IkinciAlan();
        int n = aktif.Count, ikSay = 0;
        if (ikinci != null && n > ayar.AnaEkranKapasite) ikSay = Math.Min(n - ayar.AnaEkranKapasite, ayar.IkinciEkranKapasite);
        anaSayi = n - ikSay;

        diziliyor = true;
        karolar.Clear();
        var anaAlan = CalismaAlani();
        anaPlan = PlanYap(IcAlan(anaAlan), anaSayi);
        var anaKarolar = Izgara(sahneAna, anaAlan, anaPlan, Duzen(anaPlan));
        for (int i = 0; i < anaSayi; i++) karolar[aktif[i]] = anaKarolar[i];
        if (ikinci != null && ikSay > 0)
        {
            var ikPlan = PlanYap(IcAlan(ikinci.Value), ikSay);
            var ikKarolar = Izgara(sahneIkinci, ikinci.Value, ikPlan, null);
            for (int i = 0; i < ikSay; i++) karolar[aktif[anaSayi + i]] = ikKarolar[i];
        }
        Yerlestir(anim && ayar.AnimasyonMs > 0);
        diziliyor = false;

        if (anaSayi > 0) sahneAna.Yerles(anaAlan); else sahneAna.Hide();
        gorevDugmesi.Yerles(anaAlan);
        if (ikinci != null && ikSay > 0) sahneIkinci.Yerles(ikinci.Value); else sahneIkinci.Hide();
        GrubuOneAl(Yonetilen(odak) && karolar.ContainsKey(odak) ? odak : IntPtr.Zero);
        sahneAnim = anim;
        SahneyiGuncelle();
        sahneAnim = false;
        GorevSenkron();
        TepsiYazisi();
    }

    // ---------- animation: windows glide from where they are (new ones grow from their tile's centre) ----------

    readonly System.Windows.Forms.Timer animZamanlayici = new() { Interval = 4 };
    readonly Dictionary<IntPtr, (Rectangle bas, Rectangle son)> animler = new();
    readonly HashSet<IntPtr> yeniler = new();
    DateTime animBaslangic;
    bool sahneAnim;

    void Yerlestir(bool anim)
    {
        animZamanlayici.Stop();
        animler.Clear();
        foreach (var (h, k) in karolar)
        {
            var son = k.Terminal;
            if (!anim) { Yerlestir(h, son); continue; }
            Rectangle bas;
            if (yeniler.Contains(h)) { bas = Rectangle.Inflate(son, -son.Width * 3 / 10, -son.Height * 3 / 10); Yerlestir(h, bas); }
            else bas = N.GorunenCerceve(h);
            if (bas == son) continue;
            animler[h] = (bas, son);
        }
        yeniler.Clear();
        if (animler.Count == 0) return;
        animBaslangic = DateTime.Now;
        N.timeBeginPeriod(1); // 1 ms timer resolution only while windows move
        animZamanlayici.Start();
    }

    void AnimAdim()
    {
        double t = Math.Min(1, (DateTime.Now - animBaslangic).TotalMilliseconds / Math.Max(1, ayar.AnimasyonMs));
        double e = 1 - Math.Pow(1 - t, 3); // ease-out cubic
        var toplu = N.BeginDeferWindowPos(animler.Count);
        foreach (var (h, (a, b)) in animler)
        {
            if (!N.IsWindow(h)) continue;
            int L(int x, int y) => (int)Math.Round(x + (y - x) * e);
            var r = Rectangle.FromLTRB(L(a.Left, b.Left), L(a.Top, b.Top), L(a.Right, b.Right), L(a.Bottom, b.Bottom));
            var (l, tp, rr, bb) = N.Kenarlar(h);
            if (toplu != IntPtr.Zero)
                toplu = N.DeferWindowPos(toplu, h, IntPtr.Zero, r.X - l, r.Y - tp, r.Width + l + rr, r.Height + tp + bb, N.SWP_NOZORDER | N.SWP_NOACTIVATE | N.SWP_NOOWNERZORDER);
            else Yerlestir(h, r);
        }
        if (toplu != IntPtr.Zero) N.EndDeferWindowPos(toplu);
        if (t >= 1)
        {
            animZamanlayici.Stop(); N.timeEndPeriod(1);
            foreach (var h in animler.Keys) if (N.IsWindow(h)) Yerlestir(h, animler[h].son);
            animler.Clear();
            GrubuOneAl(Yonetilen(odak) && karolar.ContainsKey(odak) ? odak : IntPtr.Zero);
        }
    }

    static void Yerlestir(IntPtr h, Rectangle r)
    {
        int yuvarlak = 2; // DWMWCP_ROUND
        N.DwmSetWindowAttribute(h, N.DWMWA_WINDOW_CORNER_PREFERENCE, ref yuvarlak, 4);
        var (l, t, rr, b) = N.Kenarlar(h);
        N.SetWindowPos(h, IntPtr.Zero, r.X - l, r.Y - t, r.Width + l + rr, r.Height + t + b,
            N.SWP_NOZORDER | N.SWP_NOACTIVATE | N.SWP_NOOWNERZORDER);
    }

    // ---------- z-order: the scene and its terminals always move together ----------

    /// <summary>
    /// Scene directly below the terminals: when a Claude comes forward the whole group comes with it,
    /// so another app never ends up wedged between the scene and the terminals.
    /// </summary>
    void GrubuOneAl(IntPtr ust, bool enUste = false)
    {
        uint f = N.SWP_NOMOVE | N.SWP_NOSIZE | N.SWP_NOACTIVATE | N.SWP_NOOWNERZORDER;
        foreach (var s in new[] { sahneAna, sahneIkinci })
        {
            if (!s.Visible) continue;
            var grup = karolar.Where(kv => kv.Value.Sahne == s).Select(kv => kv.Key).ToList();
            if (grup.Count == 0) continue;
            var bas = grup.Contains(ust) ? ust : EnUstteki(grup);
            if (bas == IntPtr.Zero) continue;
            if (enUste) N.SetWindowPos(bas, IntPtr.Zero, 0, 0, 0, 0, f); // HWND_TOP: above whatever covered it
            N.SetWindowPos(s.Handle, bas, 0, 0, 0, 0, f);
            foreach (var h in grup.Where(h => h != bas)) N.SetWindowPos(h, bas, 0, 0, 0, 0, f);
        }
    }

    /// <summary>Low-level mouse hook: only notes where the press landed; the work happens on a short timer.</summary>
    IntPtr FareKanca(int code, IntPtr w, IntPtr l)
    {
        if (code >= 0 && (w == N.WM_LBUTTONDOWN || w == N.WM_RBUTTONDOWN || w == N.WM_MBUTTONDOWN))
        {
            var p = Marshal.PtrToStructure<N.POINT>(l); // MSLLHOOKSTRUCT starts with pt
            var kok = N.GetAncestor(N.WindowFromPoint(p), N.GA_ROOT);
            if (karolar.ContainsKey(kok) || kok == sahneAna.Handle || kok == sahneIkinci.Handle)
            {
                tiklanan = kok;
                oneAlZ.Stop(); oneAlZ.Start();
            }
        }
        return N.CallNextHookEx(fareKanca, code, w, l);
    }

    void TiklamaSonrasi()
    {
        var kok = tiklanan; tiklanan = IntPtr.Zero;
        if (karolar.ContainsKey(kok)) { GrubuOneAl(kok, true); return; }
        // click on the scene (gap, title strip, top bar): the last used Claude leads the group
        var hedef = Yonetilen(odak) && karolar.ContainsKey(odak) ? odak
                  : Yonetilen(sonOdak) && karolar.ContainsKey(sonOdak) ? sonOdak
                  : karolar.Keys.FirstOrDefault();
        if (hedef == IntPtr.Zero) return;
        GrubuOneAl(hedef, true);
    }

    static IntPtr EnUstteki(List<IntPtr> grup)
    {
        IntPtr bulunan = IntPtr.Zero;
        N.EnumWindows((h, _) => { if (grup.Contains(h)) { bulunan = h; return false; } return true; }, IntPtr.Zero);
        return bulunan;
    }

    // ---------- taskbar: one button for the whole group ----------

    readonly System.Windows.Forms.Timer gorevZ = new() { Interval = 250 };
    readonly System.Windows.Forms.Timer onizlemeZ = new() { Interval = 3000 };
    Bitmap? sonGoruntu; // the work area the last time the group was in front (preview while it is hidden)
    readonly Queue<string> sonOnler = new(); // recent foreground window classes, for the click log

    bool GrupOnde() => karolar.ContainsKey(N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT));

    void OnizlemeYakala(int genislik = 1280)
    {
        if (karolar.Count == 0) return;
        var alan = sahneAna.Visible ? sahneAna.Bounds : CalismaAlani();
        var eski = sonGoruntu;
        sonGoruntu = G.Yakala(alan, genislik);
        eski?.Dispose();
    }

    Bitmap? TamOnizleme(Size boyut)
    {
        if (GrupOnde()) OnizlemeYakala(boyut.Width);
        return sonGoruntu == null ? null : sonGoruntu.Size == boyut ? new Bitmap(sonGoruntu) : G.Boyutla(sonGoruntu, boyut);
    }

    Bitmap? Onizleme(Size max)
    {
        if (GrupOnde()) OnizlemeYakala();
        return sonGoruntu == null ? null : G.Sigdir(sonGoruntu, max);
    }

    static bool GorevCubuguSinifi(string s) => s is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd" or "TaskListThumbnailWnd"
        or "XamlExplorerHostIslandWindow" or "TopLevelWindowForOverflowXamlIsland" or "NotifyIconOverflowWindow" or "ForegroundStaging";

    /// <summary>
    /// Tiled terminals have no taskbar button of their own; the Clauding button stands for them.
    /// Detached ones, and ones you minimized one by one, keep theirs so they can be found.
    /// </summary>
    void GorevSenkron()
    {
        bool tek = basladi && ayar.TekGorevDugmesi && !duraklat;
        var dizili = sira.Where(h => !ayrik.Contains(h) && N.IsWindow(h)).ToList();
        if (grupKucuk && dizili.Any(h => !N.IsIconic(h))) grupKucuk = false;
        foreach (var h in sira.ToList())
        {
            if (!N.IsWindow(h)) continue;
            bool gizle = tek && !ayrik.Contains(h) && (grupKucuk || !N.IsIconic(h));
            if (gizle) { Gorev.Gizle(h); gorevdenGizli.Add(h); } // repeated on purpose: Explorer sometimes puts a button back
            else if (gorevdenGizli.Remove(h)) Gorev.Goster(h);
        }
        if (tek && !gorevDugmesi.Visible) { gorevDugmesi.Yerles(CalismaAlani()); gorevDugmesi.Ac(); }
        else if (!tek && gorevDugmesi.Visible) gorevDugmesi.Hide();
    }

    /// <summary>
    /// The Clauding taskbar button, like any app's button: hidden group → bring it back; group in front → hide it;
    /// group behind another app → bring it forward. "In front" = the last window you were in (ignoring the taskbar
    /// itself) was one of the tiled Claudes.
    /// </summary>
    void GorevTiklandi()
    {
        if (!basladi) { KurulumAc(true); return; }
        if (sira.Count == 0) { YeniClaude(); return; }
        var dizili = sira.Where(h => !ayrik.Contains(h) && N.IsWindow(h)).ToList();
        IntPtr Hedef(IEnumerable<IntPtr> aday)
        {
            var l = aday.ToList();
            return l.Contains(odak) ? odak : l.Contains(sonOdak) ? sonOdak : l.FirstOrDefault();
        }
        string karar;
        if (dizili.Count == 0)
        {
            karar = "only detached: focus one";
            var a = Hedef(sira); if (N.IsIconic(a)) N.ShowWindow(a, N.SW_RESTORE); N.SetForegroundWindow(a);
        }
        else if (dizili.All(N.IsIconic))
        {
            karar = "hidden: restore";
            grupKucuk = false;
            foreach (var h in dizili) N.ShowWindow(h, 4 /*SW_SHOWNOACTIVATE*/);
            var hedef = Hedef(dizili);
            N.SetForegroundWindow(hedef);
            grupOndeydi = true;
            DizIste(true);
        }
        else if (grupOndeydi)
        {
            karar = "in front: hide";
            OnizlemeYakala(4096); gorevDugmesi.OnizlemeEskidi(); // the preview shows what you hid (full size, for peek)
            grupKucuk = true; grupOndeydi = false;
            foreach (var h in dizili) N.ShowWindow(h, 7 /*SW_SHOWMINNOACTIVE: keystrokes must not land in a hidden Claude*/);
            var alt = AlttakiPencere();
            if (alt == IntPtr.Zero) alt = GetShellWindow(); // nothing open below: the desktop
            N.SetForegroundWindow(alt); // like Windows does: the app below gets the keyboard
            DizIste();
        }
        else
        {
            karar = "behind: bring forward";
            var on = Hedef(dizili.Where(h => karolar.ContainsKey(h)));
            if (on == IntPtr.Zero) on = Hedef(dizili);
            foreach (var h in dizili.Where(N.IsIconic)) N.ShowWindow(h, 4 /*SW_SHOWNOACTIVATE*/);
            N.SetForegroundWindow(on);
            GrubuOneAl(on, true);
            grupOndeydi = true;
        }
        var fg = N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT);
        Kayit.Yaz($"taskbar click → {karar}; now in front: {N.Sinif(fg)}{(sira.Contains(fg) ? "*" : "")} (recent: {string.Join(" > ", sonOnler)})");
    }

    [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int v, int size);
    [DllImport("user32.dll")] static extern IntPtr GetShellWindow();

    /// <summary>The first ordinary app window below the group (what Windows would focus after a minimize).</summary>
    IntPtr AlttakiPencere()
    {
        IntPtr bulunan = IntPtr.Zero;
        uint biz = (uint)Environment.ProcessId;
        N.EnumWindows((h, _) =>
        {
            if (!N.IsWindowVisible(h) || N.IsIconic(h) || sira.Contains(h)) return true;
            N.GetWindowThreadProcessId(h, out var pid);
            if (pid == biz) return true;
            int ex = N.GetWindowLong(h, -20 /*GWL_EXSTYLE*/);
            if ((ex & 0x80 /*TOOLWINDOW*/) != 0 || (ex & 0x08 /*TOPMOST*/) != 0 || (ex & 0x08000000 /*NOACTIVATE*/) != 0) return true;
            if (DwmGetWindowAttribute(h, 14 /*DWMWA_CLOAKED*/, out var gizli, 4) == 0 && gizli != 0) return true;
            if (N.GetWindow(h, N.GW_OWNER) != IntPtr.Zero || N.Baslik(h).Length == 0) return true;
            bulunan = h; return false;
        }, IntPtr.Zero);
        return bulunan;
    }

    // ---------- events ----------

    void Olay(IntPtr hook, uint ev, IntPtr h, int obj, int child, uint th, uint t)
    {
        if (obj != 0 || child != 0 || h == IntPtr.Zero) return;
        try
        {
            switch (ev)
            {
                case N.EVENT_OBJECT_SHOW:
                    if (N.Sinif(h) == N.KonsolSinifi && N.GetWindow(h, N.GW_OWNER) == IntPtr.Zero && Terminal.BeklenenKonsol()) konsollar.Add(h);
                    if (Uygun(h)) Ekle(h);
                    break;
                case N.EVENT_OBJECT_DESTROY:
                case N.EVENT_OBJECT_HIDE:
                    if (sira.Contains(h)) Cikar(h);
                    break;
                case N.EVENT_SYSTEM_MINIMIZESTART:
                case N.EVENT_SYSTEM_MINIMIZEEND:
                    if (sira.Contains(h)) DizIste();
                    break;
                case N.EVENT_SYSTEM_MOVESIZESTART:
                    // maximise / restore also reports a move: only a real drag of a tiled window counts
                    if (sira.Contains(h) && !N.IsZoomed(h)) { suruklenen = h; surukBas = N.GorunenCerceve(h); }
                    else suruklenen = IntPtr.Zero;
                    break;
                case N.EVENT_SYSTEM_MOVESIZEEND:
                    if (h == suruklenen) SurukleBitti(h);
                    break;
                case N.EVENT_SYSTEM_FOREGROUND:
                    var kok = N.GetAncestor(h, N.GA_ROOT);
                    if (kok == sahneAna.Handle || kok == sahneIkinci.Handle) break;
                    var sinif = N.Sinif(kok);
                    sonOnler.Enqueue(sinif + (karolar.ContainsKey(kok) ? "*" : "")); while (sonOnler.Count > 5) sonOnler.Dequeue();
                    if (!GorevCubuguSinifi(sinif)) grupOndeydi = Yonetilen(kok) && !ayrik.Contains(kok) && !N.IsIconic(kok);
                    if (gorevdenGizli.Contains(kok)) { gorevZ.Stop(); gorevZ.Start(); } // the taskbar may re-add an activated window
                    if (Yonetilen(kok))
                    {
                        odak = kok; sonOdak = kok;
                        IsaretSil(kok);
                        if (kucultler.TryGetValue(kok, out var kd)) kd.Yerles(); // the window came up over its button
                        if (karolar.ContainsKey(kok)) GrubuOneAl(kok);
                        SahneIste();
                    }
                    else if (odak != IntPtr.Zero) { odak = IntPtr.Zero; SahneIste(); }
                    break;
                case N.EVENT_OBJECT_LOCATIONCHANGE:
                    if (!sira.Contains(h)) break;
                    if (ayrik.Contains(h) && tutamaklar.TryGetValue(h, out var tt)) tt.Yerles();
                    bool z = N.IsZoomed(h);
                    if (!buyuk.TryGetValue(h, out var eski) || eski != z) { buyuk[h] = z; DizIste(true); }
                    KucultGuncelle(h, z);
                    break;
                case N.EVENT_OBJECT_NAMECHANGE:
                    if (sira.Contains(h)) { if (BaslikOku(h)) SahneIste(); }
                    break;
            }
        }
        catch (Exception e) { Kayit.Yaz("event error: " + e.Message); }
    }

    static readonly string[] VarsayilanBasliklar = { "Windows PowerShell", "PowerShell", "Command Prompt", "Komut İstemi", "MINGW64", "Administrator", "Yönetici" };

    /// <summary>Claude Code puts a spinner glyph in front of the title while it works and ✳ when idle.</summary>
    bool BaslikOku(IntPtr h)
    {
        var ham = N.Baslik(h).Trim();
        bool calisiyor = false;
        var metin = ham;
        if (ClaudeBasligi(ham))
        {
            calisiyor = ham[0] != '✳' && ham[0] != '*';
            metin = ham[2..].Trim();
        }
        else if (metin.Length == 0 || VarsayilanBasliklar.Any(v => metin.StartsWith(v, StringComparison.OrdinalIgnoreCase)) || metin.Contains(":\\"))
            metin = Metin.T("yeni");
        bool degisti = !basliklar.TryGetValue(h, out var eski) || eski != metin;
        basliklar[h] = metin;
        if (!faaliyet.TryGetValue(h, out var f) || f.calisiyor != calisiyor)
        {
            faaliyet[h] = (calisiyor, DateTime.Now);
            if (!calisiyor && kapanis.TryGetValue(h, out var kp) && kp.durum == "kaydediliyor" && (DateTime.Now - kp.zaman).TotalSeconds > 3)
                kapanis[h] = ("hazir", DateTime.Now);
            if (calisiyor && isaret.TryGetValue(h, out var i) && i.durum == "bitti") IsaretSil(h);
            degisti = true;
        }
        return degisti;
    }

    /// <summary>Drop a window on another tile = swap. Drop it outside every tile = pull it out. Else snap back.</summary>
    void SurukleBitti(IntPtr h)
    {
        suruklenen = IntPtr.Zero;
        if (!sira.Contains(h) || ayrik.Contains(h) || !karolar.ContainsKey(h)) return;
        if (N.IsZoomed(h)) return;
        var simdi = N.GorunenCerceve(h);
        if (Math.Abs(simdi.X - surukBas.X) < 12 && Math.Abs(simdi.Y - surukBas.Y) < 12) { DizIste(); return; } // not really moved
        N.GetCursorPos(out var p);
        var nokta = new Point(p.X, p.Y);
        var g = ayar.Bosluk;
        var hedef = karolar.FirstOrDefault(kv => kv.Key != h && Rectangle.Inflate(kv.Value.Hucre, g, g).Contains(nokta)).Key;
        if (hedef != IntPtr.Zero) { Takas(h, hedef); DizIste(true); return; }
        else if (!karolar.Values.Any(k => Rectangle.Inflate(k.Hucre, g, g).Contains(nokta)) && !CalismaAlani().Contains(nokta)) ayrik.Add(h);
        DizIste();
    }

    void Takas(IntPtr a, IntPtr b)
    {
        int i = sira.IndexOf(a), j = sira.IndexOf(b);
        if (i < 0 || j < 0) return;
        (sira[i], sira[j]) = (sira[j], sira[i]);
    }

    // ---------- messages from the scene ----------

    void SahneMesaji(Sahne s, JsonElement m)
    {
        string t = m.GetProperty("t").GetString() ?? "";
        IntPtr Id(string ad) => m.TryGetProperty(ad, out var v) && long.TryParse(v.GetString(), out var x) ? new IntPtr(x) : IntPtr.Zero;
        int Sayi(string ad) => m.TryGetProperty(ad, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : -1;
        switch (t)
        {
            case "odak": { var h = Id("id"); if (Yonetilen(h)) { if (N.IsIconic(h)) N.ShowWindow(h, N.SW_RESTORE); N.SetForegroundWindow(h); } break; }
            case "buyut": { var h = Id("id"); if (Yonetilen(h)) { N.ShowWindow(h, N.IsZoomed(h) ? N.SW_RESTORE : N.SW_MAXIMIZE); N.SetForegroundWindow(h); } break; }
            case "ayir": { var h = Id("id"); if (Yonetilen(h)) AyirTak(h); break; }
            case "takas": Takas(Id("a"), Id("b")); Diz(true); break;
            case "yeni": { int i = Sayi("klasor"); if (i >= 0) YeniClaude(i); else YeniClaude(); break; }
            case "klasorEkle": KlasorEkleDiyalog(); break;
            case "klasorMenu": KlasorSeciciAc(new Point(Sayi("x"), Sayi("y"))); break;
            case "gerital": HepsiniGeriTak(); break;
            case "rehber": PaneliAc(false); break;
            case "ayarlar": AyarlariAc(); break;
            case "ekran": EkranDegistir(); break;
            case "kapat": { var h = Id("id"); if (Yonetilen(h)) KapatBas(h); break; }
            case "alan": AlanAyarla(m.GetProperty("ad").GetString() ?? "tam"); break;
            case "dizilim": DizilimAyarla(m.GetProperty("ad").GetString() ?? "auto"); break;
            case "kenar": KenarSurukle(m.GetProperty("e").GetString() ?? "", m.GetProperty("x").GetDouble()); break;
            case "kenarBitti": ayar.Kaydet(); break;
            case "satir": case "sutun": OranAyarla(t, m); break;
            case "esitle": ayar.Duzenler.Remove(DuzenAnahtari(anaPlan)); ayar.Kaydet(); Diz(true); break;
        }
    }

    void KenarSurukle(string kenar, double ekran)
    {
        var a = AnaEkran().WorkingArea;
        bool dikey = kenar == "ust" || kenar == "alt";
        double f = dikey ? (ekran - a.Y) / a.Height : (ekran - a.X) / a.Width;
        f = Math.Clamp(f, 0, 1);
        foreach (var mknt in new[] { 0, 1 / 3.0, 0.5, 2 / 3.0, 1 }) if (Math.Abs(f - mknt) < 0.012) f = mknt; // magnetic thirds and halves
        const double enAzG = 0.22, enAzY = 0.28;
        switch (kenar)
        {
            case "sol": ayar.AlanSol = Math.Min(f, ayar.AlanSag - enAzG); break;
            case "sag": ayar.AlanSag = Math.Max(f, ayar.AlanSol + enAzG); break;
            case "ust": ayar.AlanUst = Math.Min(f, ayar.AlanAlt - enAzY); break;
            case "alt": ayar.AlanAlt = Math.Max(f, ayar.AlanUst + enAzY); break;
        }
        Diz();
    }

    void AlanAyarla(string ad)
    {
        (ayar.AlanSol, ayar.AlanSag) = ad switch { "sol" => (0, 0.5), "sag" => (0.5, 1), "sol23" => (0, 2 / 3.0), "sag23" => (1 / 3.0, 1), _ => (0d, 1d) };
        (ayar.AlanUst, ayar.AlanAlt) = (0d, 1d);
        ayar.Kaydet();
        Diz(true);
    }

    string AlanAdi()
    {
        if (ayar.AlanUst != 0 || ayar.AlanAlt != 1) return "ozel";
        bool E(double a, double b) => Math.Abs(a - b) < 1e-6;
        if (E(ayar.AlanSol, 0) && E(ayar.AlanSag, 1)) return "tam";
        if (E(ayar.AlanSol, 0) && E(ayar.AlanSag, 0.5)) return "sol";
        if (E(ayar.AlanSol, 0.5) && E(ayar.AlanSag, 1)) return "sag";
        if (E(ayar.AlanSol, 0) && E(ayar.AlanSag, 2 / 3.0)) return "sol23";
        if (E(ayar.AlanSol, 1 / 3.0) && E(ayar.AlanSag, 1)) return "sag23";
        return "ozel";
    }

    // ---------- the scene's state ----------

    void SahneyiGuncelle()
    {
        foreach (var s in new[] { sahneAna, sahneIkinci })
        {
            if (!s.Visible) continue;
            var alan = s == sahneAna ? CalismaAlani() : IkinciAlan() ?? Rectangle.Empty;
            var plan = s == sahneAna ? anaPlan : PlanYap(IcAlan(alan), karolar.Count(kv => kv.Value.Sahne == s));
            var simdi = DateTime.Now;
            var karoListe = karolar.Where(kv => kv.Value.Sahne == s).Select(kv =>
            {
                var (h, k) = (kv.Key, kv.Value);
                string durum = "bos"; DateTime ne = simdi;
                if (isaret.TryGetValue(h, out var i)) { durum = i.durum; ne = i.zaman; }
                else if (faaliyet.TryGetValue(h, out var f)) { durum = f.calisiyor ? "calisiyor" : "bos"; ne = f.zaman; }
                return new
                {
                    id = h.ToInt64().ToString(),
                    x = k.Hucre.X - alan.X, y = k.Hucre.Y - alan.Y, w = k.Hucre.Width, h = k.Hucre.Height,
                    r = k.Satir, c = k.Sutun, cs = k.SutunSayisi, rs = k.SatirSayisi,
                    baslik = basliklar.GetValueOrDefault(h, "Claude"),
                    durum, sn = (long)(simdi - ne).TotalSeconds,
                    odak = h == odak,
                    kapanis = kapanis.TryGetValue(h, out var kp) ? kp.durum : null,
                    titre = titret.Remove(h),
                };
            }).OrderBy(k => k.r).ThenBy(k => k.c).ToList();

            var durum = new
            {
                t = "durum",
                ikinci = s.Ikinci,
                dil = ayar.EtkinDil, tema = ayar.Tema, vurgu = ayar.Vurgu, efekt = ayar.Efekt,
                w = alan.Width, h = alan.Height,
                bosluk = ayar.Bosluk, kenar = ayar.Kenar, ust = ayar.UstBar, baslik = ayar.Baslik,
                alan = AlanAdi(), dizilim = ayar.Dizilim, yon = plan.Sutunlu ? "c" : "r",
                ekranW = AnaEkran().WorkingArea.Width, ekranH = AnaEkran().WorkingArea.Height,
                ekranSayisi = Screen.AllScreens.Length, anim = sahneAnim,
                toplam = sira.Count, ayrik = ayrik.Count,
                izin = isaret.Count(x => x.Value.durum == "izin"),
                bitti = isaret.Count(x => x.Value.durum == "bitti"),
                calisan = faaliyet.Count(x => x.Value.calisiyor && sira.Contains(x.Key)),
                klasorler = ayar.Klasorler.Select(k => new { ad = k.Gorunen, yol = k.Yol }),
                varsayilan = ayar.VarsayilanKlasor,
                kapanisMetni = !string.IsNullOrWhiteSpace(ayar.KapanisMetni),
                kisayol = new { yeni = Okunur("yeni"), ekran = Okunur("ekran"), rehber = Okunur("rehber"), ayir = Okunur("ayir"), buyut = Okunur("buyut"), kapat = Okunur("kapat") },
                karolar = karoListe,
            };
            s.Durum(JsonSerializer.Serialize(durum));
        }
    }

    /// <summary>"Ctrl+Win+Alt+OemPeriod" → "Ctrl+Win+Alt+." for tooltips.</summary>
    string Okunur(string eylem) => KisayolGoster(ayar.Klavye.GetValueOrDefault(eylem, ""));

    public static string KisayolGoster(string s) => string.Join("+", s.Split('+').Select(p => p switch
    {
        "Left" => "←", "Right" => "→", "Up" => "↑", "Down" => "↓", "Oemcomma" => ",", "OemPeriod" => ".",
        "OemOpenBrackets" => "[", "OemCloseBrackets" => "]", "Escape" => "Esc", "Return" => "Enter", _ => p,
    }));

    // ---------- hook signals ----------

    public void SinyalAl(string durum, IntPtr h)
    {
        if (durum == "ayarlar") { AyarlariAc(); return; }
        if (h == IntPtr.Zero || !N.IsWindow(h) || !basladi) return;
        if (N.Sinif(h) == N.KonsolSinifi) konsollar.Add(h); // a Claude in a classic console reported itself
        if (Uygun(h) && !sira.Contains(h)) Ekle(h);
        if (durum == "bitti" && kapanis.TryGetValue(h, out var kp) && kp.durum == "kaydediliyor") { kapanis[h] = ("hazir", DateTime.Now); SahneIste(); }
        if (durum == "temizle") { IsaretSil(h); return; }
        if (N.GetAncestor(N.GetForegroundWindow(), N.GA_ROOT) == h) return; // you are looking at it
        if (durum == "bitti" && isaret.TryGetValue(h, out var o) && o.durum == "izin") return;
        isaret[h] = (durum, DateTime.Now);
        int cr = ColorTranslator.ToWin32(Ayarlar.Renk(durum == "izin" ? ayar.RenkIzin : ayar.RenkBitti));
        N.DwmSetWindowAttribute(h, N.DWMWA_BORDER_COLOR, ref cr, 4);
        SahneIste();
        TepsiYazisi();
    }

    void IsaretSil(IntPtr h)
    {
        if (!isaret.Remove(h)) return;
        if (N.IsWindow(h)) { int def = unchecked((int)0xFFFFFFFF); N.DwmSetWindowAttribute(h, N.DWMWA_BORDER_COLOR, ref def, 4); }
        SahneIste();
        TepsiYazisi();
    }

    protected override void ExitThreadCore()
    {
        foreach (var k in kancalar) N.UnhookWinEvent(k);
        if (fareKanca != IntPtr.Zero) N.UnhookWindowsHookEx(fareKanca);
        foreach (var h in isaret.Keys.ToList()) IsaretSil(h);
        foreach (var id in kisayolEylem.Keys) N.UnregisterHotKey(msj.Handle, id);
        foreach (var h in gorevdenGizli) if (N.IsWindowVisible(h)) Gorev.Goster(h);
        gorevdenGizli.Clear();
        gorevDugmesi.Close();
        sahneAna.Close(); sahneIkinci.Close(); panel?.Close(); kurulum?.Close();
        tepsi.Visible = false;
        tepsi.Dispose();
        msj.DestroyHandle();
        Surum.CikistaUygula();
        Kayit.Yaz("quit");
        base.ExitThreadCore();
    }

    /// <summary>Message-only window: receives hotkeys and the hook signals (WM_COPYDATA).</summary>
    sealed class MesajPenceresi : NativeWindow
    {
        readonly Yonetici y;
        public MesajPenceresi(Yonetici y)
        {
            this.y = y;
            CreateHandle(new CreateParams { Caption = Program.MesajPencereAdi, Parent = N.HWND_MESSAGE });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == N.WM_HOTKEY) { y.KisayolGeldi(m.WParam.ToInt32()); return; }
            if (m.Msg == N.WM_COPYDATA)
            {
                try
                {
                    var cds = Marshal.PtrToStructure<N.COPYDATASTRUCT>(m.LParam);
                    var s = Marshal.PtrToStringUni(cds.lpData, cds.cbData / 2).TrimEnd('\0');
                    var p = s.Split('|');
                    if (p.Length == 2 && long.TryParse(p[1], out var h))
                    {
                        var durum = p[0]; var hw = new IntPtr(h);
                        y.Ertele(() => y.SinyalAl(durum, hw));
                    }
                }
                catch { }
                m.Result = new IntPtr(1);
                return;
            }
            base.WndProc(ref m);
        }
    }

    // run after the sender's SendMessage returns, so the hook process is never kept waiting
    readonly System.Windows.Forms.Timer erteleZ = new() { Interval = 1 };
    readonly Queue<Action> kuyruk = new();
    bool erteleBagli;
    void Ertele(Action a)
    {
        kuyruk.Enqueue(a);
        if (!erteleBagli) { erteleZ.Tick += (_, _) => { erteleZ.Stop(); while (kuyruk.Count > 0) kuyruk.Dequeue()(); }; erteleBagli = true; }
        erteleZ.Start();
    }
}
