using System.Text.Json;
using System.Text.Json.Serialization;

namespace Clauding;

/// <summary>Relative sizes. Satir = sizes along the primary axis (rows, or columns for column layouts);
/// Sutun[i] = sizes inside primary slot i.</summary>
class Duzen
{
    public double[] Satir { get; set; } = Array.Empty<double>();
    public double[][] Sutun { get; set; } = Array.Empty<double[]>();
}

/// <summary>A folder new Claude windows can start in.</summary>
class Klasor
{
    public string Yol { get; set; } = "";
    public string Ad { get; set; } = "";
    [JsonIgnore] public string Acik => Environment.ExpandEnvironmentVariables(Yol);
    [JsonIgnore] public string Gorunen => string.IsNullOrWhiteSpace(Ad) ? Path.GetFileName(Acik.TrimEnd('\\', '/')) is { Length: > 0 } s ? s : Acik : Ad;
}

/// <summary>%APPDATA%\Clauding\settings.json. Missing file = defaults (first run shows the onboarding).</summary>
class Ayarlar
{
    public int Surum { get; set; } = 1;
    /// <summary>"auto" follows the Windows display language (Turkish → tr, everything else → en)</summary>
    public string Dil { get; set; } = "auto";
    public bool OnboardingBitti { get; set; }

    // ---------- launching ----------
    public List<Klasor> Klasorler { get; set; } = new() { new Klasor { Yol = @"%USERPROFILE%" } };
    public int VarsayilanKlasor { get; set; }
    /// <summary>New Claude shows the folder picker first (when there is more than one folder)</summary>
    public bool KlasorSor { get; set; }
    /// <summary>pwsh | powershell | cmd | gitbash | wsl</summary>
    public string Kabuk { get; set; } = "powershell";
    /// <summary>wt (Windows Terminal) | conhost (classic console window)</summary>
    public string Terminal { get; set; } = "wt";
    public string Komut { get; set; } = "claude";
    /// <summary>open new Windows Terminal windows in focus mode (no tab row / title bar)</summary>
    public bool OdakModu { get; set; } = true;
    /// <summary>if set, Close first types this into Claude and closes after Claude answers (empty = close right away)</summary>
    public string KapanisMetni { get; set; } = "";

    // ---------- layout ----------
    /// <summary>screen the work area lives on (device name; empty = main screen)</summary>
    public string Ekran { get; set; } = "";
    /// <summary>auto | cols | main | rows</summary>
    public string Dizilim { get; set; } = "auto";
    /// <summary>advanced rules on (the values below apply only then)</summary>
    public bool Gelismis { get; set; }
    /// <summary>when a tile would get narrower than this (px) ...</summary>
    public int MinGenislik { get; set; } = 1100;
    /// <summary>... put at most this many windows in a row</summary>
    public int SatirMaks { get; set; } = 2;
    /// <summary>share of the width the main window takes in "main + stack"</summary>
    public double AnaOran { get; set; } = 0.6;
    /// <summary>layout used on portrait areas (taller than wide)</summary>
    public string DikeyDizilim { get; set; } = "rows";
    public int AnimasyonMs { get; set; } = 240;
    public int Bosluk { get; set; } = 18;
    public int Kenar { get; set; } = 18;
    public int UstBar { get; set; } = 58;
    public int Baslik { get; set; } = 38;
    public int AnaEkranKapasite { get; set; } = 8;
    public bool IkinciEkraniKullan { get; set; } = true;
    public int IkinciEkranKapasite { get; set; } = 3;
    public double AlanSol { get; set; } = 0;
    public double AlanSag { get; set; } = 1;
    public double AlanUst { get; set; } = 0;
    public double AlanAlt { get; set; } = 1;
    /// <summary>tile size ratios per layout and window count: "auto:6" → primary + secondary sizes</summary>
    public Dictionary<string, Duzen> Duzenler { get; set; } = new();

    // ---------- look ----------
    /// <summary>charcoal | ember | paper</summary>
    public string Tema { get; set; } = "charcoal";
    public string Vurgu { get; set; } = "#ff6a38";
    /// <summary>drifting light and grain behind the tiles</summary>
    public bool Efekt { get; set; } = true;
    public string RenkIzin { get; set; } = "#FF5A36";
    public string RenkBitti { get; set; } = "#2FCB6E";

    // ---------- keys ----------
    /// <summary>Macro pad: its 12 keys send F13–F24; which action each one runs. Used only when a pad is enabled.</summary>
    public bool MakroKlavye { get; set; }
    public List<string> Makro { get; set; } = new()
    {
        "yeni", "bekleyen", "izin", "durdur",
        "buyut", "ayir", "sola", "saga",
        "alan", "diz", "gizle", "kapat",
    };
    /// <summary>Keyboard: action → shortcut ("" = none). Defaults use one modifier combo for everything.</summary>
    public Dictionary<string, string> Klavye { get; set; } = VarsayilanKlavye();
    public Dictionary<string, string> Nob { get; set; } = new()
    {
        ["ustSol"] = "volumedown", ["ustBas"] = "play", ["ustSag"] = "volumeup",
        ["altSol"] = "prev", ["altBas"] = "mute", ["altSag"] = "next",
    };
    /// <summary>hold the modifier combo this long (ms) to see every shortcut; 0 = off</summary>
    public int RehberBekleme { get; set; } = 700;

    // ---------- app ----------
    public bool GuncellemeDenetle { get; set; } = true;

    public const string Mod = "Ctrl+Win+Alt";

    public static Dictionary<string, string> VarsayilanKlavye()
    {
        var k = new Dictionary<string, string>
        {
            ["yeni"] = "N", ["klasorSec"] = "", ["bekleyen"] = "W", ["izin"] = "Y", ["durdur"] = "Escape",
            ["buyut"] = "Enter", ["ayir"] = "D", ["kapat"] = "Q",
            ["odakSol"] = "Left", ["odakSag"] = "Right", ["odakYukari"] = "Up", ["odakAsagi"] = "Down",
            ["sola"] = "Oemcomma", ["saga"] = "OemPeriod",
            ["alanTam"] = "F", ["alanSol"] = "OemOpenBrackets", ["alanSag"] = "OemCloseBrackets",
            ["alanSol23"] = "", ["alanSag23"] = "", ["alan"] = "A",
            ["dizilim"] = "L", ["diz"] = "T", ["gizle"] = "H", ["duraklat"] = "P", ["rehber"] = "K", ["ekran"] = "M",
        };
        for (int i = 1; i <= 9; i++) { k["git" + i] = i.ToString(); k["yeni" + i] = ""; }
        foreach (var a in k.Keys.ToList()) if (k[a] != "") k[a] = Mod + "+" + k[a];
        return k;
    }

    public static string Klasoru => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clauding");
    public static string Yol => Path.Combine(Klasoru, "settings.json");
    // relaxed escaping keeps "+" and non-ASCII readable in a file people may open by hand
    static readonly JsonSerializerOptions Js = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [JsonIgnore] public bool IlkAcilis { get; private set; }

    public static Ayarlar Yukle()
    {
        try
        {
            if (File.Exists(Yol))
            {
                var a = JsonSerializer.Deserialize<Ayarlar>(File.ReadAllText(Yol), Js) ?? new Ayarlar();
                var v = new Ayarlar();
                if (a.Makro == null || a.Makro.Count != 12) a.Makro = v.Makro;
                foreach (var kv in v.Klavye) a.Klavye.TryAdd(kv.Key, kv.Value);
                foreach (var kv in v.Nob) a.Nob.TryAdd(kv.Key, kv.Value);
                if (a.Klasorler == null || a.Klasorler.Count == 0) a.Klasorler = v.Klasorler;
                a.VarsayilanKlasor = Math.Clamp(a.VarsayilanKlasor, 0, a.Klasorler.Count - 1);
                return a;
            }
        }
        catch (Exception e) { Kayit.Yaz("settings could not be read, using defaults: " + e.Message); }
        return new Ayarlar { IlkAcilis = true };
    }

    public void Kaydet()
    {
        try
        {
            Directory.CreateDirectory(Klasoru);
            var gecici = Yol + ".tmp";
            File.WriteAllText(gecici, JsonSerializer.Serialize(this, Js));
            File.Move(gecici, Yol, true); // never leave a half-written file behind
        }
        catch (Exception e) { Kayit.Yaz("settings could not be saved: " + e.Message); }
    }

    [JsonIgnore] public string EtkinDil => Dil is "tr" or "en" ? Dil : Donanim.SistemDili;

    [JsonIgnore] public Klasor SeciliKlasor => Klasorler[Math.Clamp(VarsayilanKlasor, 0, Klasorler.Count - 1)];

    public static Color Renk(string hex) { try { return ColorTranslator.FromHtml(hex); } catch { return Color.OrangeRed; } }

    /// <summary>"Ctrl+Win+Alt+Left" → (modifiers, virtual key)</summary>
    public static bool KisayolCoz(string s, out uint mods, out uint vk)
    {
        mods = N.MOD_NOREPEAT; vk = 0;
        foreach (var p in s.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (p.ToLowerInvariant())
            {
                case "win": mods |= N.MOD_WIN; break;
                case "alt": mods |= N.MOD_ALT; break;
                case "ctrl": case "control": mods |= N.MOD_CONTROL; break;
                case "shift": mods |= N.MOD_SHIFT; break;
                default:
                    if (p.Length == 1 && char.IsDigit(p[0])) { vk = (uint)Keys.D0 + (uint)(p[0] - '0'); break; }
                    if (!Enum.TryParse<Keys>(p, true, out var k)) return false;
                    vk = (uint)k; break;
            }
        }
        return vk != 0;
    }
}
