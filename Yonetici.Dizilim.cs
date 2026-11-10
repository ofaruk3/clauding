using System.Text.Json;

namespace Clauding;

/// <summary>
/// A layout is a set of primary slots (rows, or columns when Sutunlu) each split into secondary slots.
/// Presets: auto grid, columns, main + stack (column layout), rows. Advanced rule: when tiles in a row
/// would get narrower than MinGenislik, rows hold at most SatirMaks windows. Portrait areas use their own preset.
/// </summary>
record Plan(bool Sutunlu, int[] Ikincil, string Ad)
{
    public int Birincil => Ikincil.Length;
    public int Toplam => Ikincil.Sum();
}

sealed partial class Yonetici
{
    Rectangle IcAlan(Rectangle alan) =>
        new(alan.X + ayar.Kenar, alan.Y + ayar.UstBar, alan.Width - 2 * ayar.Kenar, alan.Height - ayar.UstBar - ayar.Kenar);

    /// <summary>Which shape n windows take in this (inner) area.</summary>
    Plan PlanYap(Rectangle ic, int n) => PlanHesapla(ic.Width, ic.Height, n, ayar.Dizilim, ayar.Gelismis, ayar.DikeyDizilim, ayar.MinGenislik, ayar.SatirMaks, ayar.Bosluk);

    public static Plan PlanHesapla(int W, int H, int n, string dizilim, bool gelismis, string dikeyDizilim, int minGenislik, int satirMaks, int bosluk)
    {
        if (n <= 0) return new Plan(false, Array.Empty<int>(), dizilim);
        bool dikeyAlan = H > W * 0.9;
        var ad = dikeyAlan ? (gelismis ? dikeyDizilim : "rows") : dizilim;
        if (n == 1) return new Plan(false, new[] { 1 }, ad);

        static int[] Dagit(int n, int parca) // n windows over `parca` slots, earlier slots take the extra
        {
            var r = new int[parca];
            for (int i = 0; i < parca; i++) r[i] = n / parca + (i < n % parca ? 1 : 0);
            return r;
        }

        int[] satirlar;
        switch (ad)
        {
            case "main":
            {
                int kalan = n - 1, yiginSutun = kalan > 4 ? 2 : 1;
                var s = new List<int> { 1 };
                s.AddRange(Dagit(kalan, yiginSutun));
                return new Plan(true, s.ToArray(), ad);
            }
            case "rows":
                satirlar = n <= 4 ? Enumerable.Repeat(1, n).ToArray() : Dagit(n, (int)Math.Ceiling(n / 2.0));
                break;
            case "cols":
                satirlar = new[] { n };
                break;
            default: // auto
                if (dikeyAlan) satirlar = n <= 3 ? Enumerable.Repeat(1, n).ToArray() : Dagit(n, (int)Math.Ceiling(n / 2.0));
                else
                {
                    int r = n <= 3 ? 1 : n <= 8 ? 2 : n <= 12 ? 3 : (int)Math.Ceiling(Math.Sqrt(n / 2.0));
                    satirlar = Dagit(n, r);
                }
                break;
        }

        // advanced rule: narrow tiles → cap windows per row
        if (gelismis && satirMaks >= 1)
        {
            int enCok = satirlar.Max();
            double genislik = (W - bosluk * (enCok - 1)) / (double)enCok;
            if (enCok > satirMaks && genislik < minGenislik)
                satirlar = Dagit(n, (int)Math.Ceiling(n / (double)satirMaks));
        }
        return new Plan(false, satirlar, ad);
    }

    string DuzenAnahtari(Plan p) => $"{p.Ad}:{(p.Sutunlu ? "c" : "r")}:{string.Join("-", p.Ikincil)}";

    Duzen? Duzen(Plan p)
    {
        if (p.Toplam <= 1) return null;
        if (ayar.Duzenler.TryGetValue(DuzenAnahtari(p), out var d)) return d;
        if (p.Ad == "main" && p.Sutunlu && p.Birincil >= 2)
        {
            // the main column takes AnaOran, stack columns share the rest
            double kalan = (1 - ayar.AnaOran) / (p.Birincil - 1);
            return new Duzen { Satir = new[] { ayar.AnaOran }.Concat(Enumerable.Repeat(kalan, p.Birincil - 1)).ToArray() };
        }
        return null;
    }

    /// <summary>Tiles inside the scene: top bar, side margins, gaps; each tile = header strip + terminal.</summary>
    List<Karo> Izgara(Sahne s, Rectangle alan, Plan p, Duzen? d)
    {
        var sonuc = new List<Karo>();
        if (p.Toplam <= 0) return sonuc;
        int g = ayar.Bosluk, hb = ayar.Baslik;
        var ic = IcAlan(alan);
        var birW = Oranlar(d?.Satir, p.Birincil);
        var birincil = p.Sutunlu ? Bol(ic.X, ic.Width, g, birW) : Bol(ic.Y, ic.Height, g, birW);
        for (int i = 0; i < p.Birincil; i++)
        {
            var ikW = Oranlar(d != null && d.Sutun.Length == p.Birincil ? d.Sutun[i] : null, p.Ikincil[i]);
            var ikincil = p.Sutunlu ? Bol(ic.Y, ic.Height, g, ikW) : Bol(ic.X, ic.Width, g, ikW);
            for (int j = 0; j < p.Ikincil[i]; j++)
            {
                var hucre = p.Sutunlu
                    ? Rectangle.FromLTRB(birincil[i].a, ikincil[j].a, birincil[i].b, ikincil[j].b)
                    : Rectangle.FromLTRB(ikincil[j].a, birincil[i].a, ikincil[j].b, birincil[i].b);
                var term = Rectangle.FromLTRB(hucre.Left, hucre.Top + hb, hucre.Right, hucre.Bottom);
                // Satir/Sutun always mean visual row/column for the scene; it reads `yon` to know which axis is primary
                sonuc.Add(p.Sutunlu
                    ? new Karo(s, hucre, term, j, i, p.Birincil, p.Ikincil[i])
                    : new Karo(s, hucre, term, i, j, p.Ikincil[i], p.Birincil));
            }
        }
        return sonuc;
    }

    static double[] Oranlar(double[]? w, int n) =>
        w != null && w.Length == n && w.All(x => x > 0) ? w : Enumerable.Repeat(1.0, n).ToArray();

    static (int a, int b)[] Bol(int bas, int boy, int g, double[] w)
    {
        double toplam = w.Sum(), net = boy - g * (w.Length - 1), x = bas;
        var r = new (int, int)[w.Length];
        for (int i = 0; i < w.Length; i++)
        {
            double uz = net * w[i] / toplam;
            r[i] = ((int)Math.Round(x), (int)Math.Round(x + uz));
            x += uz + g;
        }
        return r;
    }

    /// <summary>A divider was dragged: "satir" = primary sizes, "sutun" (+ r = primary index) = secondary sizes.</summary>
    void OranAyarla(string tur, JsonElement m)
    {
        var p = anaPlan;
        if (p.Toplam <= 1) return;
        var d = ayar.Duzenler.GetValueOrDefault(DuzenAnahtari(p)) ?? Duzen(p) ?? new Duzen();
        d = new Duzen { Satir = d.Satir, Sutun = d.Sutun };
        if (d.Satir.Length != p.Birincil) d.Satir = Enumerable.Repeat(1.0, p.Birincil).ToArray();
        if (d.Sutun.Length != p.Birincil || d.Sutun.Select((x, i) => x.Length != p.Ikincil[i]).Any(b => b))
            d.Sutun = p.Ikincil.Select(c => Enumerable.Repeat(1.0, c).ToArray()).ToArray();
        var w = m.GetProperty("w").EnumerateArray().Select(x => Math.Max(0.05, x.GetDouble())).ToArray();
        if (tur == "satir" && w.Length == p.Birincil) d.Satir = w;
        if (tur == "sutun")
        {
            int r = m.GetProperty("r").GetInt32();
            if (r >= 0 && r < p.Birincil && w.Length == p.Ikincil[r]) d.Sutun[r] = w;
        }
        ayar.Duzenler[DuzenAnahtari(p)] = d;
        if (m.TryGetProperty("son", out var son) && son.GetBoolean()) ayar.Kaydet();
        Diz();
    }

    static readonly string[] DizilimSirasi = { "auto", "cols", "main", "rows" };

    void DizilimAyarla(string ad)
    {
        if (!DizilimSirasi.Contains(ad)) return;
        ayar.Dizilim = ad;
        ayar.Kaydet();
        Diz(true);
    }

    void SonrakiDizilim() => DizilimAyarla(DizilimSirasi[(Array.IndexOf(DizilimSirasi, ayar.Dizilim) + 1) % DizilimSirasi.Length]);
}
