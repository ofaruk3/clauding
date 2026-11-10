namespace Clauding;

/// <summary>UI strings drawn by C# (tray menu, balloons, picker). The web pages carry their own (web/dil.js).</summary>
static class Metin
{
    public static string Dil = "en";

    static readonly Dictionary<string, (string en, string tr)> S = new()
    {
        ["yeni"] = ("New Claude", "Yeni Claude"),
        ["geriTak"] = ("attach", "geri tak"),
        ["buyutK"] = ("maximize", "büyüt"),
        ["yeniIn"] = ("New Claude in", "Yeni Claude:"),
        ["diz"] = ("Re-tile, bring detached back", "Yeniden diz, ayrılanları geri tak"),
        ["bekleyen"] = ("Go to the waiting Claude", "Bekleyen Claude'a git"),
        ["duraklat"] = ("Pause tiling", "Dizmeyi duraklat"),
        ["baslangic"] = ("Start with Windows", "Windows ile başlat"),
        ["kisayollar"] = ("Shortcuts…", "Kısayollar…"),
        ["ayarlar"] = ("Settings…", "Ayarlar…"),
        ["guncelle"] = ("Check for updates", "Güncellemeleri denetle"),
        ["guncelVar"] = ("Update {0} ready, restart to install", "{0} güncellemesi hazır, kurmak için yeniden başlat"),
        ["guncelYok"] = ("You have the latest version", "En son sürümdesin"),
        ["guncelHata"] = ("Could not check for updates", "Güncellemeler denetlenemedi"),
        ["yenidenBaslat"] = ("Restart to update", "Güncellemek için yeniden başlat"),
        ["sorun"] = ("Report a problem…", "Sorun bildir…"),
        ["sorunHazir"] = ("Report saved to your desktop: {0}", "Rapor masaüstüne kaydedildi: {0}"),
        ["cikis"] = ("Quit", "Çıkış"),
        ["pencere"] = ("{0} windows", "{0} pencere"),
        ["ayrik"] = ("{0} detached", "{0} ayrık"),
        ["izinBek"] = ("{0} waiting for permission", "{0} izin bekliyor"),
        ["bitti"] = ("{0} done", "{0} bitti"),
        ["duraklatildi"] = ("paused", "duraklatıldı"),
        ["kisayolDolu"] = ("These shortcuts are taken by another app, change them in Shortcuts:\n{0}", "Şu kısayollar başka bir uygulamada kayıtlı, Kısayollar'dan değiştir:\n{0}"),
        ["terminalYok"] = ("Could not open a terminal: {0}", "Terminal açılamadı: {0}"),
        ["klasorYok"] = ("Folder not found: {0}", "Klasör bulunamadı: {0}"),
        ["klasorEkle"] = ("Add folder…", "Klasör ekle…"),
        ["klasorSecBaslik"] = ("Choose a folder for new Claude windows", "Yeni Claude'lar için bir klasör seç"),
        ["kancaYok"] = ("Status hooks are off: Clauding can't see when a Claude waits for you. Turn them on in Settings.", "Durum hook'ları kapalı: Clauding bir Claude'un seni beklediğini göremez. Ayarlar'dan aç."),
        ["calisiyor"] = ("Clauding is running. Press {0} for a new Claude, hold {1} to see every shortcut.", "Clauding çalışıyor. Yeni Claude için {0}, bütün kısayollar için {1} basılı tut."),
    };

    public static string T(string k) => S.TryGetValue(k, out var v) ? (Dil == "tr" ? v.tr : v.en) : k;
    public static string T(string k, params object[] a) => string.Format(T(k), a);
}
