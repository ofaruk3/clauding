namespace Clauding;

/// <summary>Every action a key can run (shortcuts panel, macro pad) and what a knob can send. English + Turkish.</summary>
static class Katalog
{
    public record Eylem(string id, string grup, string en, string enAc, string tr, string trAc)
    {
        public string Ad(string dil) => dil == "tr" ? tr : en;
        public string Aciklama(string dil) => dil == "tr" ? trAc : enAc;
    }
    public record MedyaTusu(string id, string en, string tr) { public string Ad(string dil) => dil == "tr" ? tr : en; }

    public static readonly Eylem[] Eylemler = Olustur().ToArray();

    static IEnumerable<Eylem> Olustur()
    {
        yield return new("yeni", "claude", "New Claude", "Opens a Claude in your default folder (or the folder picker, if it is on)",
                                           "Yeni Claude", "Varsayılan klasörde Claude açar (klasör seçici açıksa önce onu gösterir)");
        yield return new("klasorSec", "claude", "Folder picker", "Pick the folder for a new Claude",
                                                "Klasör seçici", "Yeni Claude için klasör seç");
        for (int i = 1; i <= 9; i++)
            yield return new("yeni" + i, "klasor", $"New Claude in folder {i}", $"Opens a Claude in folder {i} of your list",
                                                   $"{i}. klasörde yeni Claude", $"Listendeki {i}. klasörde Claude açar");
        yield return new("bekleyen", "claude", "Go to waiting", "Jumps to the Claude that waits for you, permission requests first",
                                               "Bekleyene git", "Seni bekleyen Claude'a geçer, önce izin isteyene");
        yield return new("izin", "claude", "Approve", "Jumps to the Claude asking for permission and presses Enter",
                                           "İzin ver", "İzin bekleyen Claude'a geçip Enter'a basar");
        yield return new("durdur", "claude", "Interrupt", "Sends Esc to the Claude you are in",
                                             "Durdur", "Baktığın Claude'a Esc gönderir");
        yield return new("kapat", "claude", "Close", "Closes the Claude you are in (press twice quickly to skip the close message)",
                                            "Kapat", "Baktığın Claude'u kapatır (kapanış mesajını atlamak için iki kez hızlı bas)");

        foreach (var (id, en, tr) in new[] { ("odakSol", "left", "sola"), ("odakSag", "right", "sağa"), ("odakYukari", "up", "yukarı"), ("odakAsagi", "down", "aşağı") })
            yield return new(id, "odak", $"Focus {en}", $"Moves focus to the Claude {en} of this one",
                                         $"Odak {tr}", $"Odağı {tr} komşu Claude'a taşır");
        for (int i = 1; i <= 9; i++)
            yield return new("git" + i, "odak", $"Go to Claude {i}", $"Focuses tile {i} (in reading order)",
                                                $"{i}. Claude'a git", $"{i}. karoya geçer (okuma sırasıyla)");
        yield return new("sola", "odak", "Move earlier", "Swaps the active Claude with the one before it",
                                         "Sırada geri al", "Aktif Claude'u bir öncekiyle yer değiştirir");
        yield return new("saga", "odak", "Move later", "Swaps the active Claude with the one after it",
                                         "Sırada ileri al", "Aktif Claude'u bir sonrakiyle yer değiştirir");

        yield return new("buyut", "pencere", "Maximize", "Full screen for the active Claude; press again to put it back",
                                             "Büyüt", "Aktif Claude'u tam ekran yapar, tekrar basınca geri koyar");
        yield return new("ayir", "pencere", "Detach / attach", "Takes the active Claude out of the grid or puts it back",
                                            "Ayır / tak", "Aktif Claude'u ızgaradan çıkarır ya da geri takar");
        yield return new("diz", "pencere", "Re-tile", "Puts detached windows back and tiles everything again",
                                           "Yeniden diz", "Ayrılanları geri takar, her şeyi yeniden dizer");
        yield return new("gizle", "pencere", "Hide / show all", "Minimizes every Claude, or brings them all back",
                                             "Gizle / göster", "Bütün Claude'ları küçültür ya da geri getirir");

        yield return new("alanTam", "alan", "Area: full screen", "Tiles use the whole screen", "Alan: tam ekran", "Karolar bütün ekranı kullanır");
        yield return new("alanSol", "alan", "Area: left half", "Tiles move to the left half", "Alan: sol yarı", "Karolar sol yarıya geçer");
        yield return new("alanSag", "alan", "Area: right half", "Tiles move to the right half", "Alan: sağ yarı", "Karolar sağ yarıya geçer");
        yield return new("alanSol23", "alan", "Area: left two thirds", "Tiles use the left ⅔", "Alan: sol ⅔", "Karolar soldaki ⅔'ü kullanır");
        yield return new("alanSag23", "alan", "Area: right two thirds", "Tiles use the right ⅔", "Alan: sağ ⅔", "Karolar sağdaki ⅔'ü kullanır");
        yield return new("alan", "alan", "Cycle area", "Full → left half → right half", "Alan değiştir", "Tam → sol yarı → sağ yarı");
        yield return new("ekran", "alan", "Other display", "Moves the whole work area to the next display", "Öbür ekran", "Bütün çalışma alanını sıradaki ekrana taşır");

        yield return new("dizilim", "dizilim", "Next layout", "Auto grid → columns → main + stack → rows", "Sonraki dizilim", "Otomatik ızgara → sütunlar → ana + yığın → satırlar");
        yield return new("dizilimAuto", "dizilim", "Layout: auto grid", "", "Dizilim: otomatik ızgara", "");
        yield return new("dizilimCols", "dizilim", "Layout: columns", "", "Dizilim: sütunlar", "");
        yield return new("dizilimMain", "dizilim", "Layout: main + stack", "", "Dizilim: ana + yığın", "");
        yield return new("dizilimRows", "dizilim", "Layout: rows", "", "Dizilim: satırlar", "");

        yield return new("duraklat", "uygulama", "Pause tiling", "Stops or resumes automatic tiling", "Duraklat", "Otomatik dizmeyi durdurur ya da sürdürür");
        yield return new("rehber", "uygulama", "Shortcuts", "Opens the shortcuts panel; press again to close it", "Kısayollar", "Kısayollar panelini açar, tekrar basınca kapatır");
        yield return new("bos", "uygulama", "Nothing", "The key does nothing", "Boş", "Tuş hiçbir şey yapmaz");
    }

    public static readonly (string id, string en, string tr)[] Gruplar =
    {
        ("claude", "Claude", "Claude"), ("odak", "Focus and order", "Odak ve sıra"), ("pencere", "Windows", "Pencereler"),
        ("alan", "Work area", "Çalışma alanı"), ("dizilim", "Layout", "Dizilim"), ("klasor", "Folders", "Klasörler"),
        ("uygulama", "App", "Uygulama"),
    };

    public static readonly MedyaTusu[] Medya =
    {
        new("volumedown", "Volume down", "Ses kıs"), new("volumeup", "Volume up", "Ses aç"), new("mute", "Mute", "Sesi kapat"),
        new("play", "Play / pause", "Oynat / duraklat"), new("prev", "Previous track", "Önceki şarkı"), new("next", "Next track", "Sonraki şarkı"),
        new("stop", "Stop", "Müziği durdur"),
    };
}
