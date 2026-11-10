"use strict";
// Strings for the scene and the shortcuts panel. The host sends `dil` ("en" | "tr") with every state.
window.DIL = {
  en: {
    pencere: "windows", calisiyor: "working", izinBekliyor: "waiting for you", bitti: "done", tasanlar: "overflow",
    simdi: "just now", dk: "min", sa: "h", once: "ago", bos: "idle",
    metaCalisiyor: "working · {0}", metaIzin: "needs you", metaBitti: "done · {0}",
    alan: "area", tam: "full", sol: "left", sag: "right",
    dizilim: "layout", dAuto: "Auto grid", dCols: "Columns", dMain: "Main + stack", dRows: "Rows",
    oburEkran: "other display", oburEkranAc: "Move the work area to the other display",
    kisayollar: "shortcuts", kisayollarAc: "Every shortcut",
    ayarlarAc: "Settings", yeniClaude: "new claude", yeniAc: "New Claude in your default folder", klasorAc: "Pick a folder",
    ayrikGeri: "{0} detached · bring back",
    ayir: "Detach", buyut: "Maximize",
    kapat: "close", kaydediliyor: "closing…", simdiKapat: "close now",
    kapatIpucu: "Close", kapatIpucuMetin: "Once: sends your closing line · twice: closes right away",
    ayirDisari: "detach: {0}", genislik: "width", yukseklik: "height",
  },
  tr: {
    pencere: "pencere", calisiyor: "çalışıyor", izinBekliyor: "seni bekliyor", bitti: "bitti", tasanlar: "taşanlar",
    simdi: "az önce", dk: "dk", sa: "sa", once: "önce", bos: "boşta",
    metaCalisiyor: "çalışıyor · {0}", metaIzin: "seni bekliyor", metaBitti: "bitti · {0}",
    alan: "alan", tam: "tam ekran", sol: "sol yarı", sag: "sağ yarı",
    dizilim: "dizilim", dAuto: "Otomatik ızgara", dCols: "Sütunlar", dMain: "Ana + yığın", dRows: "Satırlar",
    oburEkran: "öbür ekran", oburEkranAc: "Çalışma alanını öbür ekrana taşı",
    kisayollar: "kısayollar", kisayollarAc: "Bütün kısayollar",
    ayarlarAc: "Ayarlar", yeniClaude: "yeni claude", yeniAc: "Varsayılan klasörde yeni Claude", klasorAc: "Klasör seç",
    ayrikGeri: "{0} ayrık · geri al",
    ayir: "Ayır", buyut: "Büyüt",
    kapat: "kapat", kaydediliyor: "kapanıyor…", simdiKapat: "şimdi kapat",
    kapatIpucu: "Kapat", kapatIpucuMetin: "Bir kez: kapanış satırını yazar · iki kez: hemen kapatır",
    ayirDisari: "ayır: {0}", genislik: "genişlik", yukseklik: "yükseklik",
  },
};
window.dil = "en";
window.T = (k, ...a) => {
  let s = (DIL[window.dil] && DIL[window.dil][k]) || DIL.en[k] || k;
  a.forEach((v, i) => (s = s.replace("{" + i + "}", v)));
  return s;
};
window.dilUygula = (kok = document) => {
  kok.querySelectorAll("[data-i18n]").forEach((el) => (el.textContent = T(el.dataset.i18n)));
  kok.querySelectorAll("[data-i18n-title]").forEach((el) => (el.title = T(el.dataset.i18nTitle)));
  document.documentElement.lang = window.dil;
};
