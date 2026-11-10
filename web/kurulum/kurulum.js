// Clauding onboarding (first run: one decision per card, Raycast-style) and Settings (the same cards
// behind a sidebar). Every change is sent to the app right away ({t:"ayar", degerler:{...}}).
"use strict";

const host = window.chrome && window.chrome.webview;
const gonder = (m) => host && host.postMessage(m);
const $ = (s, el = document) => el.querySelector(s);
const html = (s) => { const t = document.createElement("template"); t.innerHTML = s.trim(); return t.content.firstElementChild; };
const MOD = new URLSearchParams(location.search).get("mod") === "ayarlar" ? "ayarlar" : "onboarding";
if (MOD === "ayarlar") document.body.classList.add("ayarlar");

// ---------- state: settings (from the app) + what was detected ----------
let durum = {
  dil: "auto", kabuk: "powershell", terminal: "wt", klasorler: [{ yol: "%USERPROFILE%\\Projects", ad: "Projects" }], varsayilan: 0, klasorSor: false,
  dizilim: "auto", gelismis: false, minGenislik: 1100, satirMaks: 2, anaOran: 0.6, bosluk: 18, dikeyDizilim: "rows",
  tasma: 8, ikinciEkran: true, tema: "charcoal", vurgu: "#ff6a38", efekt: true, animasyonMs: 240, guncelle: true,
  makroKlavye: false, kapanisMetni: "", odakModu: true, rehberBekleme: 700,
};
let algi = {
  claude: "C:\\Users\\you\\.local\\bin\\claude.exe", wt: true, webview2: "142.0", makro: false, kanca: false, baslangic: false,
  kabuklar: [{ id: "pwsh", ad: "PowerShell 7", var: true }, { id: "powershell", ad: "Windows PowerShell", var: true }, { id: "cmd", ad: "Command Prompt", var: true }, { id: "gitbash", ad: "Git Bash", var: true }, { id: "wsl", ad: "WSL", var: false }],
  ekranlar: [{ w: 3840, h: 2160, ana: true }, { w: 1080, h: 1920, ana: false }], surum: "0.1.0", kurulu: false,
};
let kisayol = { yeni: "Ctrl+Win+Alt+N" };
let dil = "en";
let modKomb = "Ctrl+Win+Alt";

const ayarla = (degerler) => { Object.assign(durum, degerler); gonder({ t: "ayar", degerler }); };

// ---------- strings ----------
const S = {
  en: {
    back: "Back", skip: "Skip setup", next: "Continue", start: "Get started", finish: "Open first Claude",
    welcomeT: "Clauding", welcomeD: "Every Claude Code window, tiled, tracked and one keystroke away.", lang: "Language", auto: "Auto",
    checkT: "Checking your setup", checkD: "Clauding drives the tools you already have.",
    cc: "Claude Code", ccS: "found", ccMiss: "not found on PATH", ccGet: "Install Claude Code",
    wt: "Windows Terminal", wtS: "tabs, panes, GPU text", wtMiss: "not found · the classic console will be used", wtGet: "Get it",
    wv: "WebView2 Runtime", wvS: "draws the overlay", rec: "Recommended", notInstalled: "not installed",
    termT: "Where should Claude run?", termD: "Pick the shell new Claude windows start in.",
    host: "Opens in", hostWT: "Windows Terminal", hostCon: "Classic console", focusMode: "Windows Terminal focus mode (no tab bar)",
    sh: { pwsh: "PowerShell 7", powershell: "Windows PowerShell", cmd: "Command Prompt", gitbash: "Git Bash", wsl: "WSL" },
    wslNote: "WSL runs Claude inside Linux; status hooks are not available there yet.",
    folderT: "Your project folders", folderD: "Add every folder you work in. The starred one opens by default; add its number to pick another.",
    ask: "Show the folder picker on New Claude", askS: "Off: the starred folder opens right away", addFolder: "Add folder", defaultF: "default", makeDefault: "Make default", remove: "Remove", newIn: "New Claude in",
    hookT: "Let Clauding see Claude's state", hookD: "A tiny hook tells Clauding when a Claude is working, waiting for you or done.",
    hookOn: "Install status hooks", hookNote: "Added to <b>~/.claude/settings.json</b> next to your own hooks, never replacing them. A backup is kept, and uninstalling removes them.",
    stWork: "working", stWait: "needs you", stDone: "done",
    keyT: "Your shortcut key", keyD: "One modifier combo for everything. Hold it for a second to see every shortcut.",
    modCWA: "Ctrl + Win + Alt", modCW: "Ctrl + Win", clash: "Ctrl + Win + arrows switches virtual desktops; Clauding skips those.",
    kNew: "New Claude", kFocus: "Move focus", kJump: "Jump to 1–9", kWait: "Go to waiting", kArea: "Full / left / right half", kMax: "Maximize",
    kMore: "Every action can get its own key.", openShortcuts: "Open Shortcuts", tryIt: "Try it: press the keys",
    macro: "<b>Macro pad detected.</b> Its 12 keys and 2 knobs can be mapped in Shortcuts.", macroOn: "Use the macro pad (F13–F24)",
    layT: "How should windows tile?", layD: "Start simple. Advanced rules work like a tiling WM, per display.",
    simple: "Simple", adv: "Advanced", wins: "windows",
    lay: { auto: "Auto grid", cols: "Columns", main: "Main + stack", rows: "Rows" },
    land: "Landscape", port: "Portrait",
    r1a: "If a tile gets narrower than", r1b: "px, put at most", r1c: "per row",
    r2a: "Main window takes", r2b: "of the width", r3a: "Gap between tiles", r4a: "Portrait displays use",
    advNote: "Rules apply per display. Dividers between tiles can still be dragged; double-click one to reset.",
    scrT: "Use your second display", scrD: "When the main display fills up, new Claudes flow onto the other one.",
    over: "Move to the other display after", overB: "windows", moveKey: "Send everything across:", useSecond: "Use the second display",
    main: "Main", second: "Second", oneScreen: "One display found. Plug in another and Clauding picks it up.",
    lookT: "Make it yours", lookD: "Theme and accent change the overlay, frames and this window.",
    th: { charcoal: "Charcoal", ember: "Ember", paper: "Paper" }, accent: "Accent", fx: "Background light and grain", fxS: "Turn off on laptops to save battery",
    speed: "Animation", sp: { smooth: "Smooth", fast: "Fast", off: "Off" },
    doneT: "You're set", doneD: "Press the shortcut or use the tray icon. Everything here is in Settings later.",
    login: "Start with Windows", upd: "Check for updates", updS: "From GitHub Releases, installs on next launch",
    sum: { shell: "Shell", folder: "Folder", keys: "Shortcut", layout: "Layout" }, rules: "rules",
    ver: "Clauding {0} · by ofc.works",
    nav: { genel: "General", terminal: "Terminal", folder: "Folders", hooks: "Status hooks", keys: "Shortcuts", layout: "Layout", screens: "Displays", look: "Appearance", about: "About" },
    genelT: "General", genelD: "Language, startup and how Close behaves.",
    closeLine: "Closing line", closeLineS: "If set, Close first types this into Claude (e.g. a save-memory prompt) and closes after Claude answers. Empty = close right away.",
    holdT: "Hold the modifiers to see shortcuts", holdOff: "Off",
    aboutT: "Clauding", aboutD: "A tiling window manager for Claude Code on Windows. Open source.",
    checkNow: "Check now", restartUpd: "Restart to update to {0}", devBuild: "Development build: updates are off.",
    report: "Report a problem", reportS: "Zips the log and settings to your desktop.", openSettings: "Settings folder", openLogs: "Logs",
    repo: "GitHub", errHook: "Could not change the hooks: {0}",
  },
  tr: {
    back: "Geri", skip: "Kurulumu atla", next: "Devam", start: "Başlayalım", finish: "İlk Claude'u aç",
    welcomeT: "Clauding", welcomeD: "Bütün Claude Code pencereleri dizili, takipte ve bir tuş uzağında.", lang: "Dil", auto: "Otomatik",
    checkT: "Kurulum kontrol ediliyor", checkD: "Clauding zaten sende olan araçları kullanır.",
    cc: "Claude Code", ccS: "bulundu", ccMiss: "PATH'te bulunamadı", ccGet: "Claude Code'u kur",
    wt: "Windows Terminal", wtS: "sekme, bölme, GPU yazı", wtMiss: "bulunamadı · klasik konsol kullanılacak", wtGet: "İndir",
    wv: "WebView2", wvS: "kaplamayı çizer", rec: "Önerilen", notInstalled: "kurulu değil",
    termT: "Claude nerede çalışsın?", termD: "Yeni Claude pencerelerinin açılacağı kabuğu seç.",
    host: "Açıldığı yer", hostWT: "Windows Terminal", hostCon: "Klasik konsol", focusMode: "Windows Terminal odak modu (sekme çubuğu yok)",
    sh: { pwsh: "PowerShell 7", powershell: "Windows PowerShell", cmd: "Komut İstemi", gitbash: "Git Bash", wsl: "WSL" },
    wslNote: "WSL'de Claude Linux içinde çalışır; durum hook'ları orada henüz yok.",
    folderT: "Proje klasörlerin", folderD: "Çalıştığın bütün klasörleri ekle. Yıldızlı olan varsayılan açılır; başka biri için numarasına bas.",
    ask: "Yeni Claude'da klasör seçiciyi göster", askS: "Kapalı: yıldızlı klasör hemen açılır", addFolder: "Klasör ekle", defaultF: "varsayılan", makeDefault: "Varsayılan yap", remove: "Kaldır", newIn: "Yeni Claude:",
    hookT: "Clauding Claude'un durumunu görsün", hookD: "Küçük bir hook, Claude çalışırken, seni beklerken ya da bitince Clauding'e haber verir.",
    hookOn: "Durum hook'larını kur", hookNote: "<b>~/.claude/settings.json</b>'a senin hook'larının yanına eklenir, hiçbirini silmez. Yedek alınır, kaldırınca temizlenir.",
    stWork: "çalışıyor", stWait: "seni bekliyor", stDone: "bitti",
    keyT: "Kısayol tuşun", keyD: "Her şey için tek bir tuş kombinasyonu. Bir saniye basılı tut, bütün kısayollar görünsün.",
    modCWA: "Ctrl + Win + Alt", modCW: "Ctrl + Win", clash: "Ctrl + Win + oklar sanal masaüstü değiştirir; Clauding onları kullanmaz.",
    kNew: "Yeni Claude", kFocus: "Odağı taşı", kJump: "1–9'a atla", kWait: "Bekleyene git", kArea: "Tam / sol / sağ yarı", kMax: "Büyüt",
    kMore: "Her işe ayrı tuş atanabilir.", openShortcuts: "Kısayolları aç", tryIt: "Dene: tuşlara bas",
    macro: "<b>Makro klavye bulundu.</b> 12 tuşu ve 2 nobu Kısayollar'dan atanabilir.", macroOn: "Makro klavyeyi kullan (F13–F24)",
    layT: "Pencereler nasıl dizilsin?", layD: "Basit başla. Gelişmiş kurallar ekran başına, bir tiling WM gibi çalışır.",
    simple: "Basit", adv: "Gelişmiş", wins: "pencere",
    lay: { auto: "Otomatik ızgara", cols: "Sütunlar", main: "Ana + yığın", rows: "Satırlar" },
    land: "Yatay", port: "Dikey",
    r1a: "Bir karo şundan darsa", r1b: "px, bir satıra en çok", r1c: "pencere koy",
    r2a: "Ana pencere genişliğin", r2b: "kadarını alsın", r3a: "Karolar arası boşluk", r4a: "Dikey ekranlarda",
    advNote: "Kurallar ekran başına uygulanır. Karolar arasındaki çizgiler yine sürüklenebilir; çift tık sıfırlar.",
    scrT: "İkinci ekranı da kullan", scrD: "Ana ekran dolunca yeni Claude'lar öbür ekrana akar.",
    over: "Şu kadar pencereden sonra öbür ekrana geç:", overB: "", moveKey: "Hepsini öbür ekrana at:", useSecond: "İkinci ekranı kullan",
    main: "Ana", second: "İkinci", oneScreen: "Tek ekran bulundu. Başka bir ekran takınca Clauding onu da kullanır.",
    lookT: "Kendine göre ayarla", lookD: "Tema ve vurgu rengi kaplamayı, çerçeveleri ve bu pencereyi değiştirir.",
    th: { charcoal: "Kömür", ember: "Kor", paper: "Kâğıt" }, accent: "Vurgu", fx: "Arka plan ışığı ve gren", fxS: "Laptopta pil için kapatabilirsin",
    speed: "Animasyon", sp: { smooth: "Akıcı", fast: "Hızlı", off: "Kapalı" },
    doneT: "Hazırsın", doneD: "Kısayola bas ya da tepsi simgesini kullan. Buradaki her şey sonra Ayarlar'da.",
    login: "Windows ile başlat", upd: "Güncellemeleri denetle", updS: "GitHub Releases'tan, sonraki açılışta kurulur",
    sum: { shell: "Kabuk", folder: "Klasör", keys: "Kısayol", layout: "Dizilim" }, rules: "kurallar",
    ver: "Clauding {0} · ofc.works",
    nav: { genel: "Genel", terminal: "Terminal", folder: "Klasörler", hooks: "Durum hook'ları", keys: "Kısayollar", layout: "Dizilim", screens: "Ekranlar", look: "Görünüm", about: "Hakkında" },
    genelT: "Genel", genelD: "Dil, açılış ve Kapat'ın davranışı.",
    closeLine: "Kapanış satırı", closeLineS: "Doluysa Kapat önce bunu Claude'a yazar (ör. hafızayı kaydet), Claude cevap verince kapatır. Boş = hemen kapatır.",
    holdT: "Kısayolları görmek için basılı tutma", holdOff: "Kapalı",
    aboutT: "Clauding", aboutD: "Windows'ta Claude Code için tiling pencere yöneticisi. Açık kaynak.",
    checkNow: "Şimdi denetle", restartUpd: "{0} sürümüne geçmek için yeniden başlat", devBuild: "Geliştirme sürümü: güncellemeler kapalı.",
    report: "Sorun bildir", reportS: "Kayıtları ve ayarları masaüstüne zipler.", openSettings: "Ayar klasörü", openLogs: "Kayıtlar",
    repo: "GitHub", errHook: "Hook'lar değiştirilemedi: {0}",
  },
};
const t = () => S[dil] || S.en;
const f = (s, ...a) => { a.forEach((v, i) => (s = s.replace("{" + i + "}", v))); return s; };

// ---------- tiling preview (same rules as the app: Yonetici.Dizilim.cs) ----------
function plan(n, W, H, o) {
  if (n <= 0) return { sutunlu: false, ik: [] };
  const dikeyAlan = H > W * 0.9;
  const ad = dikeyAlan ? (o.gelismis ? o.dikeyDizilim : "rows") : o.dizilim;
  if (n === 1) return { sutunlu: false, ik: [1] };
  const dagit = (n, p) => Array.from({ length: p }, (_, i) => Math.floor(n / p) + (i < n % p ? 1 : 0));
  let s;
  if (ad === "main") { const k = n - 1; return { sutunlu: true, ik: [1, ...dagit(k, k > 4 ? 2 : 1)], ad }; }
  if (ad === "rows") s = n <= 4 ? Array(n).fill(1) : dagit(n, Math.ceil(n / 2));
  else if (ad === "cols") s = [n];
  else if (dikeyAlan) s = n <= 3 ? Array(n).fill(1) : dagit(n, Math.ceil(n / 2));
  else s = dagit(n, n <= 3 ? 1 : n <= 8 ? 2 : n <= 12 ? 3 : Math.ceil(Math.sqrt(n / 2)));
  if (o.gelismis && o.satirMaks >= 1) {
    const enCok = Math.max(...s), gen = (W - o.bosluk * (enCok - 1)) / enCok;
    if (enCok > o.satirMaks && gen < o.minGenislik) s = dagit(n, Math.ceil(n / o.satirMaks));
  }
  return { sutunlu: false, ik: s, ad };
}
function yerlesim(n, W, H, o) {
  const p = plan(n, W, H, o), g = o.bosluk, out = [];
  const P = p.ik.length;
  const anaW = p.ad === "main" && p.sutunlu ? [o.anaOran, ...Array(P - 1).fill((1 - o.anaOran) / (P - 1))] : Array(P).fill(1);
  const bol = (bas, boy, w) => { const top = w.reduce((a, b) => a + b, 0), net = boy - g * (w.length - 1); let x = bas; return w.map((v) => { const u = net * v / top, r = [x, x + u]; x += u + g; return r; }); };
  const bir = p.sutunlu ? bol(0, W, anaW) : bol(0, H, anaW);
  p.ik.forEach((c, i) => {
    const ik = p.sutunlu ? bol(0, H, Array(c).fill(1)) : bol(0, W, Array(c).fill(1));
    ik.forEach(([a, b]) => out.push(p.sutunlu ? { x: bir[i][0], y: a, w: bir[i][1] - bir[i][0], h: b - a } : { x: a, y: bir[i][0], w: b - a, h: bir[i][1] - bir[i][0] }));
  });
  return out;
}
function karolariCiz(kap, n, gW, gH, gercekW, gercekH, opts, durumlar = []) {
  const kx = gW / gercekW, ky = gH / gercekH;
  const rects = yerlesim(n, gercekW, gercekH, { ...durum, ...opts, bosluk: (opts.bosluk ?? durum.bosluk) * 3 });
  const mevcut = [...kap.querySelectorAll(".karo")];
  rects.forEach((r, i) => {
    let el = mevcut[i];
    if (!el) {
      el = html(`<div class="karo" style="opacity:0"><i class="nk"></i><i class="sat" style="top:16px;width:46%"></i><i class="sat" style="top:24px;width:62%"></i><i class="sat" style="top:32px;width:34%"></i></div>`);
      el.style.left = (r.x + r.w / 2) * kx + "px"; el.style.top = (r.y + r.h / 2) * ky + "px"; el.style.width = "0px"; el.style.height = "0px";
      kap.appendChild(el);
      el.getBoundingClientRect();
    }
    el.style.opacity = 1;
    el.style.left = r.x * kx + "px"; el.style.top = r.y * ky + "px";
    el.style.width = Math.max(r.w * kx, 4) + "px"; el.style.height = Math.max(r.h * ky, 4) + "px";
    el.className = "karo " + (durumlar[i] || "");
  });
  mevcut.slice(rects.length).forEach((el) => { el.style.opacity = 0; setTimeout(() => el.remove(), 400); });
}

// ---------- helpers ----------
const temalar = { charcoal: { zemin: "#0e0e0d", kart: "#1c1c1a" }, ember: { zemin: "#120b08", kart: "#23160f" }, paper: { zemin: "#ecebe6", kart: "#ffffff" } };
const vurgular = ["#ff6a38", "#d97757", "#2fcb6e", "#4c8dff", "#e8e4dc"];
const hizlar = { smooth: 240, fast: 140, off: 0 };
const hizAdi = () => (durum.animasyonMs === 0 ? "off" : durum.animasyonMs <= 160 ? "fast" : "smooth");
const kisaAd = (y) => y.replace(/[\\/]+$/, "").split(/[\\/]/).pop() || y;

function satirKontrol(ad, alt, i, ok) {
  return `<div class="kontrol-satir" style="animation-delay:${i * 0.12}s"><span class="ad">${ad}<small>${alt}</small></span><i class="durum-i" data-ok="${ok ? 1 : 0}"></i></div>`;
}
function gosterim(k) { return k === "anaOran" ? Math.round(durum[k] * 100) + "%" : durum[k]; }
function sayi(k, adim) { return `<span class="sayi" data-k="${k}" data-adim="${adim}"><button data-y="-">−</button><b>${gosterim(k)}</b><button data-y="+">+</button></span>`; }
const SINIR = { minGenislik: [400, 2400], satirMaks: [1, 6], anaOran: [0.3, 0.8], bosluk: [0, 40], tasma: [1, 16] };
function sayiBagla(kap, sonra) {
  kap.querySelectorAll(".sayi[data-k]").forEach((s) => s.addEventListener("click", (e) => {
    const b = e.target.closest("button"); if (!b) return;
    const key = s.dataset.k, adim = +s.dataset.adim, y = b.dataset.y === "+" ? 1 : -1, [lo, hi] = SINIR[key];
    ayarla({ [key]: Math.min(hi, Math.max(lo, +(durum[key] + y * adim).toFixed(2))) });
    s.querySelector("b").textContent = gosterim(key); sonra && sonra();
  }));
}
function ikonSvg(k) {
  const r = (x, y, w, h) => `<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="2"/>`;
  const s = { auto: r(2, 2, 30, 14) + r(34, 2, 30, 14) + r(2, 18, 20, 14) + r(24, 18, 19, 14) + r(45, 18, 19, 14), cols: r(2, 2, 14, 30) + r(18, 2, 14, 30) + r(34, 2, 14, 30) + r(50, 2, 14, 30), main: r(2, 2, 38, 30) + r(42, 2, 22, 9) + r(42, 13, 22, 8) + r(42, 23, 22, 9), rows: r(20, 2, 26, 9) + r(20, 13, 26, 8) + r(20, 23, 26, 9) };
  return `<svg viewBox="0 0 66 34">${s[k]}</svg>`;
}
function segment(el, sec, deger, sonra) {
  const kap = $(sec, el); if (!kap) return;
  const isaretle = (v) => kap.querySelectorAll("button").forEach((b) => b.classList.toggle("secili", b.dataset.v === v));
  isaretle(deger);
  kap.addEventListener("click", (e) => { const b = e.target.closest("button"); if (!b || b.disabled) return; isaretle(b.dataset.v); sonra(b.dataset.v); });
}
function secim(el, sec, deger, sonra, cocuk) {
  const kap = $(sec, el);
  const isaretle = (v) => kap.querySelectorAll(cocuk).forEach((b) => b.classList.toggle("secili", b.dataset.v === v));
  isaretle(deger);
  kap.addEventListener("click", (e) => { const b = e.target.closest(cocuk); if (!b || b.disabled) return; isaretle(b.dataset.v); sonra(b.dataset.v); });
}
function anahtar(el, sec, deger, sonra) { const b = $(sec, el); if (!b) return; b.classList.toggle("acik", !!deger); b.addEventListener("click", () => { const v = !b.classList.contains("acik"); b.classList.toggle("acik", v); sonra(v); }); }
function temaUygula() {
  document.documentElement.dataset.tema = durum.tema;
  document.documentElement.style.setProperty("--vurgu", durum.vurgu);
  document.documentElement.style.setProperty("--vurgu-koyu", `color-mix(in srgb, ${durum.vurgu} 80%, #000)`);
}
const ikinciVar = () => algi.ekranlar.length > 1;

// ---------- cards ----------
const kartlar = [
  {
    id: "welcome", birincil: "start", nav: null, sadece: "onboarding",
    ciz: () => `
      <div class="gorsel"><div class="mini-ekran" id="w-ekran"></div><div class="logo-yazi">by ofc<span>.</span>works</div></div>
      <div class="icerik">
        <h2>${t().welcomeT}</h2>
        <p class="aciklama">${t().welcomeD}</p>
        <div class="kontrol" style="display:flex;align-items:center;gap:12px;justify-content:space-between">
          <span class="not">${t().lang}</span>
          <div class="segment" id="dil"><button data-v="auto">${t().auto}</button><button data-v="en">English</button><button data-v="tr">Türkçe</button></div>
        </div>
      </div>`,
    bagla(el) {
      segment(el, "#dil", durum.dil, (v) => { ayarla({ dil: v }); dil = v === "auto" ? sistemDili : v; yenidenCiz(); });
      const ekran = $("#w-ekran", el);
      const adimlar = [1, 2, 3, 4, 6, 4, 2];
      const st = [["calisiyor"], ["calisiyor", "bitti"], ["calisiyor", "izin", "bitti"], ["bitti", "izin", "calisiyor", ""], ["calisiyor", "", "bitti", "izin", "", "calisiyor"], ["izin", "calisiyor", "bitti", ""], ["bitti", "calisiyor"]];
      let i = 0;
      const tur = () => { karolariCiz(ekran, adimlar[i], 378, 184, 3840, 1972, { dizilim: "auto", gelismis: false, bosluk: 6 }, st[i]); i = (i + 1) % adimlar.length; };
      tur();
      return setInterval(tur, 1500);
    },
  },
  {
    id: "genel", nav: "genel", sadece: "ayarlar",
    ciz: () => `
      <div class="icerik" style="gap:12px">
        <h2>${t().genelT}</h2>
        <p class="aciklama">${t().genelD}</p>
        <div style="display:flex;align-items:center;gap:12px;justify-content:space-between">
          <span>${t().lang}</span>
          <div class="segment" id="dil"><button data-v="auto">${t().auto}</button><button data-v="en">English</button><button data-v="tr">Türkçe</button></div>
        </div>
        <div class="anahtar-satir"><span class="metin">${t().login}</span><button class="anahtar" id="bas"></button></div>
        <div class="anahtar-satir"><span class="metin">${t().holdT}</span>
          <div class="segment" id="tut"><button data-v="0">${t().holdOff}</button><button data-v="500">0.5 s</button><button data-v="700">0.7 s</button><button data-v="1200">1.2 s</button></div></div>
        <div class="anahtar-satir" style="display:block"><span class="metin">${t().closeLine}<small>${t().closeLineS}</small></span>
          <input class="metin-alan" id="kapanis" style="margin-top:8px" spellcheck="false" value="${(durum.kapanisMetni || "").replace(/"/g, "&quot;")}"></div>
      </div>`,
    bagla(el) {
      segment(el, "#dil", durum.dil, (v) => { ayarla({ dil: v }); dil = v === "auto" ? sistemDili : v; yenidenCiz(); });
      anahtar(el, "#bas", algi.baslangic, (v) => gonder({ t: "baslangic", acik: v }));
      segment(el, "#tut", String(durum.rehberBekleme), (v) => ayarla({ rehberBekleme: +v }));
      let z = 0;
      $("#kapanis", el).addEventListener("input", (e) => { clearTimeout(z); z = setTimeout(() => ayarla({ kapanisMetni: e.target.value }), 400); });
    },
  },
  {
    id: "check", sadece: "onboarding",
    ciz: () => `
      <div class="gorsel"><div class="kontroller">
        ${satirKontrol(t().cc, algi.claude ? t().ccS : t().ccMiss, 0, !!algi.claude)}
        ${satirKontrol(t().wt, algi.wt ? t().wtS : t().wtMiss, 1, algi.wt)}
        ${satirKontrol(t().wv, algi.webview2 ? t().wvS + " · " + algi.webview2 : "", 2, !!algi.webview2)}
      </div></div>
      <div class="icerik">
        <h2>${t().checkT}</h2>
        <p class="aciklama">${t().checkD}</p>
        ${!algi.claude ? `<div class="not"><span>${t().cc}: <b>${t().ccMiss}</b></span><a class="baglanti" data-url="https://docs.anthropic.com/en/docs/claude-code/setup" style="margin-left:auto">${t().ccGet}</a></div>` : ""}
        ${!algi.wt ? `<div class="not"><span>${t().wt}: <b>${t().wtMiss}</b></span><a class="baglanti" data-url="ms-windows-store://pdp/?productid=9N0DX20HK701" style="margin-left:auto">${t().wtGet}</a></div>` : ""}
      </div>`,
    bagla(el) {
      const zam = [];
      el.querySelectorAll(".durum-i").forEach((d, i) => zam.push(setTimeout(() => d.classList.add(d.dataset.ok === "1" ? "tamam" : "eksik"), 650 + i * 450)));
      return () => zam.forEach(clearTimeout);
    },
  },
  {
    id: "terminal", nav: "terminal",
    ciz: () => {
      const ikon = { pwsh: "&gt;_", powershell: "PS", cmd: "C:\\", gitbash: "$", wsl: "λ" };
      return `
      <div class="gorsel">
        <div class="ana-sec"><span class="not">${t().host}</span><div class="segment" id="ana"><button data-v="wt" ${algi.wt ? "" : "disabled"}>${t().hostWT}</button><button data-v="conhost">${t().hostCon}</button></div></div>
        <div class="kabuk-buyuk">${["pwsh", "powershell", "cmd", "gitbash"].map((k) => `<div data-k="${k}">${ikon[k]}</div>`).join("")}</div>
        <div class="komut" id="komut"></div>
      </div>
      <div class="icerik">
        <h2>${t().termT}</h2>
        <p class="aciklama">${t().termD}</p>
        <div class="secenekler kabuk3" id="kabuklar">
          ${algi.kabuklar.map((k) => `
            <button class="secenek" data-v="${k.id}" ${k.var ? "" : "disabled"}>
              <span class="ikon">${ikon[k.id]}</span>
              <span><span class="ad">${t().sh[k.id] || k.ad}</span><br><span class="alt-yazi">${!k.var ? t().notInstalled : k.id === "pwsh" ? t().rec : k.id}</span></span>
            </button>`).join("")}
        </div>
        ${MOD === "ayarlar" ? `<div class="anahtar-satir"><span class="metin">${t().focusMode}</span><button class="anahtar" id="odak"></button></div>` : ""}
        <p class="not" id="wslNot" ${durum.kabuk === "wsl" ? "" : "hidden"}><span>${t().wslNote}</span></p>
      </div>`;
    },
    bagla(el) {
      if (!algi.wt && durum.terminal === "wt") ayarla({ terminal: "conhost" });
      const komut = () => {
        const d = (durum.klasorler[durum.varsayilan] || {}).yol || "%USERPROFILE%";
        const kab = { pwsh: `pwsh -NoLogo -NoExit -Command <em>claude</em>`, powershell: `powershell -NoLogo -NoExit -Command <em>claude</em>`, cmd: `cmd /k <em>claude</em>`,
          gitbash: `bash --login -i -c "<em>claude</em>; exec bash"`, wsl: `wsl --cd "${d}" -- bash -lic "<em>claude</em>"` }[durum.kabuk];
        $("#komut", el).innerHTML = durum.terminal === "wt" ? `<b>wt</b> -w new nt -d "${d}" ${kab}` : `<b>conhost</b> ${kab}`;
        el.querySelectorAll(".kabuk-buyuk div").forEach((x) => x.classList.toggle("secili", x.dataset.k === durum.kabuk));
        const w = $("#wslNot", el); if (w) w.hidden = durum.kabuk !== "wsl";
      };
      secim(el, "#kabuklar", durum.kabuk, (v) => { ayarla({ kabuk: v }); komut(); }, ".secenek");
      segment(el, "#ana", durum.terminal, (v) => { ayarla({ terminal: v }); komut(); });
      anahtar(el, "#odak", durum.odakModu, (v) => ayarla({ odakModu: v }));
      komut();
    },
  },
  {
    id: "folder", nav: "folder", sinif: "kisa",
    ciz: () => `
      <div class="gorsel"><div class="klasor-g sol"><div class="sekme"></div><div class="arka"></div><div class="kagit"></div><div class="on"></div></div>
        <div class="secici" id="secici"></div></div>
      <div class="icerik" style="gap:10px">
        <h2>${t().folderT}</h2>
        <p class="aciklama">${t().folderD}</p>
        <div class="klasorler" id="klasorler"></div>
        <div class="kontrol anahtar-satir" style="padding-bottom:0"><span class="metin">${t().ask}<small>${t().askS}</small></span><button class="anahtar" id="sor"></button></div>
      </div>`,
    bagla(el) {
      const liste = () => {
        $("#klasorler", el).innerHTML = durum.klasorler.map((k, i) => `
          <div class="klasor-satir ${i === durum.varsayilan ? "varsayilan" : ""}" data-i="${i}">
            <button class="yildiz" title="${t().makeDefault}">${i === durum.varsayilan ? "★" : "☆"}</button>
            <span class="yol">${k.yol}</span>
            <span class="ad2">${i === durum.varsayilan ? t().defaultF : ""}</span>
            ${i < 9 ? `<kbd>N</kbd><kbd>${i + 1}</kbd>` : ""}
            <button class="sil" title="${t().remove}" ${durum.klasorler.length > 1 ? "" : "disabled"}>×</button>
          </div>`).join("") + `<button class="ekle" id="ekle">+ ${t().addFolder}</button>`;
        $("#secici", el).innerHTML = `<div class="bas">${t().newIn} ${modKomb.split("+").map((p) => `<kbd>${p}</kbd>`).join("")}<kbd>N</kbd></div>` +
          durum.klasorler.slice(0, 4).map((k, i) => `<div class="oge ${i === durum.varsayilan ? "su" : ""}"><span>${k.ad || kisaAd(k.yol)}</span><kbd>${i + 1}</kbd></div>`).join("");
        $("#ekle", el).onclick = () => gonder({ t: "klasorEkle" });
      };
      $("#klasorler", el).addEventListener("click", (e) => {
        const satir = e.target.closest(".klasor-satir"); if (!satir) return;
        const i = +satir.dataset.i;
        if (e.target.closest(".yildiz")) ayarla({ varsayilan: i });
        if (e.target.closest(".sil") && durum.klasorler.length > 1) {
          const yeni = durum.klasorler.filter((_, j) => j !== i);
          let v = durum.varsayilan; if (i === v) v = 0; else if (i < v) v--;
          ayarla({ klasorler: yeni, varsayilan: v });
        }
        liste();
      });
      anahtar(el, "#sor", durum.klasorSor, (v) => ayarla({ klasorSor: v }));
      liste();
      el._liste = liste;
    },
  },
  {
    id: "hooks", nav: "hooks",
    ciz: () => `
      <div class="gorsel"><div class="kanca-g">
        <div class="tek"><div class="karo calisiyor" id="h-karo"><i class="nk"></i><i class="sat" style="top:18px;width:50%"></i><i class="sat" style="top:28px;width:70%"></i><i class="sat" style="top:38px;width:40%"></i><i class="sat" style="top:48px;width:58%"></i></div><div class="etiket" id="h-et">${t().stWork}</div></div>
        <div class="kod-panel">"hooks": {<br>
          <span class="ek">"Notification": clauding --signal waiting</span>
          <span class="ek">"Stop": clauding --signal done</span>
          <span class="ek">"UserPromptSubmit": clauding --signal clear</span>
          &nbsp;&nbsp;…your hooks stay<br>}</div>
      </div></div>
      <div class="icerik">
        <h2>${t().hookT}</h2>
        <p class="aciklama">${t().hookD}</p>
        <div class="kontrol">
          <div class="anahtar-satir" style="border-top:0;padding-top:0"><span class="metin">${t().hookOn}</span><button class="anahtar" id="kanca"></button></div>
          <p class="not"><span>${t().hookNote}</span></p>
          <p class="hata-not" id="kancaHata" hidden></p>
        </div>
      </div>`,
    bagla(el) {
      anahtar(el, "#kanca", algi.kanca, (v) => { algi.kanca = v; gonder({ t: "kanca", acik: v }); });
      const seq = [["calisiyor", t().stWork], ["izin", t().stWait], ["bitti", t().stDone]];
      let i = 0;
      return setInterval(() => { i = (i + 1) % 3; $("#h-karo", el).className = "karo " + seq[i][0]; $("#h-et", el).textContent = seq[i][1]; $("#h-et", el).style.color = i === 1 ? "var(--vurgu)" : i === 2 ? "var(--yesil)" : ""; }, 1700);
    },
  },
  {
    id: "keys", nav: "keys",
    ciz: () => {
      const mods = modKomb.split("+");
      return `
      <div class="gorsel">
        <div class="tuslar" id="tuslar">${mods.map((m) => `<div class="tus ${m === "Ctrl" || m === "Alt" ? "genis" : ""}" data-k="${m}">${m}</div><span class="arti">+</span>`).join("")}<div class="tus" data-k="N">N</div></div>
        <div class="tus-ipucu">${t().tryIt}</div>
      </div>
      <div class="icerik">
        <h2>${t().keyT}</h2>
        <div class="segment" id="mod" style="align-self:flex-start"><button data-v="Ctrl+Win+Alt">${t().modCWA}</button><button data-v="Ctrl+Win">${t().modCW}</button></div>
        ${modKomb === "Ctrl+Win" ? `<p class="not"><span>${t().clash}</span></p>` : ""}
        <div class="mod-baslik">${mods.map((m) => `<kbd>${m}</kbd>`).join("")}<span>+</span></div>
        <div class="kisayol-liste">
          <div><span>${t().kNew}</span><kbd>N</kbd></div>
          <div><span>${t().kFocus}</span><kbd>←</kbd><kbd>↑</kbd><kbd>→</kbd><kbd>↓</kbd></div>
          <div><span>${t().kJump}</span><kbd>1</kbd><kbd>…</kbd><kbd>9</kbd></div>
          <div><span>${t().kWait}</span><kbd>W</kbd></div>
          <div><span>${t().kArea}</span><kbd>F</kbd><kbd>[</kbd><kbd>]</kbd></div>
          <div><span>${t().kMax}</span><kbd>Enter</kbd></div>
        </div>
        <div style="display:flex;align-items:center;justify-content:space-between;gap:10px">
          <p class="ileri-not">${t().kMore}</p><button class="kucuk-dugme" id="kisayollar">${t().openShortcuts}</button>
        </div>
        ${algi.makro ? `<div class="makro-not"><span>${t().macro}</span><button class="anahtar" id="makro" style="margin-left:auto"></button></div>` : ""}
      </div>`;
    },
    bagla(el) {
      segment(el, "#mod", modKomb, (v) => { ayarla({ mod: v }); modKomb = v; yenidenCiz(); });
      $("#kisayollar", el).onclick = () => gonder({ t: "kisayollar" });
      anahtar(el, "#makro", durum.makroKlavye, (v) => ayarla({ makroKlavye: v }));
      const ad = (e) => ({ Control: "Ctrl", Alt: "Alt", Meta: "Win", OS: "Win" }[e.key] || (e.key.length === 1 ? e.key.toUpperCase() : null));
      const bas = (e) => {
        const k = ad(e); if (!k) return;
        if (e.type === "keydown" && !["Ctrl", "Alt", "Win"].includes(k)) { const n = el.querySelector('.tus[data-k="N"]'); if (n) n.textContent = k; }
        el.querySelectorAll(".tus").forEach((x) => { if (x.dataset.k === k || (x.dataset.k === "N" && !["Ctrl", "Alt", "Win"].includes(k))) x.classList.toggle("basili", e.type === "keydown"); });
        if (e.key === "Alt") e.preventDefault();
      };
      addEventListener("keydown", bas); addEventListener("keyup", bas);
      return () => { removeEventListener("keydown", bas); removeEventListener("keyup", bas); };
    },
  },
  {
    id: "layout", nav: "layout", sinif: "uzun",
    ciz: () => `
      <div class="gorsel">
        <div class="monitor" id="mon"><div class="mon-ic" id="mon-ic" style="position:absolute;inset:12px"></div></div>
        <div class="monitor-ust" style="top:auto;bottom:10px">
          <div class="segment" id="yon"><button data-v="land">${t().land}</button><button data-v="port">${t().port}</button></div>
          <div class="sayac"><button id="az">−</button><span id="adet"></span><button id="cok">+</button></div>
        </div>
      </div>
      <div class="icerik" style="gap:12px">
        <div style="display:flex;align-items:center;justify-content:space-between">
          <h2>${t().layT}</h2>
          <div class="segment" id="mod2"><button data-v="s">${t().simple}</button><button data-v="a">${t().adv}</button></div>
        </div>
        <div id="lay-govde"></div>
      </div>`,
    bagla(el) {
      let n = 5, yon = algi.ekranlar.some((e) => e.ana && e.h > e.w) ? "port" : "land";
      const govde = () => {
        const g = $("#lay-govde", el);
        if (!durum.gelismis) {
          g.innerHTML = `<p class="aciklama" style="margin:0 0 10px">${t().layD}</p><div class="duzen-kartlar" id="duzenler">${["auto", "cols", "main", "rows"].map((k) => `<button class="duzen-kart" data-v="${k}">${ikonSvg(k)}${t().lay[k]}</button>`).join("")}</div>`;
          secim(g, "#duzenler", durum.dizilim, (v) => { ayarla({ dizilim: v }); ciz(); }, ".duzen-kart");
        } else {
          g.innerHTML = `<div class="kurallar">
            <div class="kural">${t().r1a} ${sayi("minGenislik", 100)} ${t().r1b.replace("px, ", "")} ${sayi("satirMaks", 1)} ${t().r1c}</div>
            <div class="kural">${t().r2a} ${sayi("anaOran", 0.05)} ${t().r2b}</div>
            <div class="kural">${t().r3a} ${sayi("bosluk", 2)} · ${t().r4a} <span class="sayi"><select id="dikey">${["rows", "auto", "cols", "main"].map((k) => `<option value="${k}" ${durum.dikeyDizilim === k ? "selected" : ""}>${t().lay[k]}</option>`).join("")}</select></span></div>
            <div class="duzen-kartlar" id="duzenler" style="margin-top:4px">${["auto", "cols", "main", "rows"].map((k) => `<button class="duzen-kart" data-v="${k}">${ikonSvg(k)}${t().lay[k]}</button>`).join("")}</div>
          </div><p class="ileri-not" style="margin-top:8px">${t().advNote}</p>`;
          sayiBagla(g, ciz);
          secim(g, "#duzenler", durum.dizilim, (v) => { ayarla({ dizilim: v }); ciz(); }, ".duzen-kart");
          $("#dikey", g).addEventListener("change", (e) => { ayarla({ dikeyDizilim: e.target.value }); ciz(); });
        }
      };
      const ciz = () => {
        const mon = $("#mon", el), port = yon === "port";
        const mw = port ? 124 : 440, mh = 218;
        mon.style.width = mw + "px"; mon.style.height = mh + "px";
        $("#mon-ic", el).style.inset = port ? "10px" : "12px";
        const rw = port ? 1080 : 3840, rh = port ? 1840 : 2080;
        karolariCiz($("#mon-ic", el), n, mw - 24, mh - 24, rw, rh, { bosluk: durum.bosluk / 2 });
        $("#adet", el).textContent = `${n} ${t().wins}`;
      };
      segment(el, "#yon", yon, (v) => { yon = v; ciz(); });
      segment(el, "#mod2", durum.gelismis ? "a" : "s", (v) => { ayarla({ gelismis: v === "a" }); govde(); ciz(); });
      $("#az", el).onclick = () => { n = Math.max(1, n - 1); ciz(); };
      $("#cok", el).onclick = () => { n = Math.min(9, n + 1); ciz(); };
      govde(); ciz();
    },
  },
  {
    id: "screens", nav: "screens", kosul: () => MOD === "ayarlar" || ikinciVar(),
    ciz: () => {
      const ana = algi.ekranlar.find((e) => e.ana) || algi.ekranlar[0] || { w: 1920, h: 1080 };
      const ik = algi.ekranlar.find((e) => !e.ana);
      const boyut = (e, maxW, maxH) => { const k = Math.min(maxW / e.w, maxH / e.h); return [Math.round(e.w * k), Math.round(e.h * k)]; };
      const [aw, ah] = boyut(ana, 260, 150), [iw, ih] = ik ? boyut(ik, 200, 150) : [0, 0];
      return `
      <div class="gorsel"><div class="ekranlar">
        <div class="monitor" style="width:${aw}px;height:${ah}px"><div id="e1" style="position:absolute;inset:10px"></div><span class="ad">${t().main} · ${ana.w} × ${ana.h}</span></div>
        ${ik ? `<div class="monitor" style="width:${iw}px;height:${ih}px"><div id="e2" style="position:absolute;inset:8px"></div><span class="ad">${t().second} · ${ik.w} × ${ik.h}</span></div>` : ""}
      </div></div>
      <div class="icerik">
        <h2>${t().scrT}</h2>
        <p class="aciklama">${ik ? t().scrD : t().oneScreen}</p>
        <div class="kontrol" style="display:grid;gap:10px">
          <div class="anahtar-satir" style="border-top:0;padding:0"><span class="metin">${t().useSecond}</span><button class="anahtar" id="ikinci"></button></div>
          <div class="kural">${t().over} ${sayi("tasma", 1)} ${t().overB}</div>
          <div class="not"><span>${t().moveKey}</span> ${modKomb.split("+").map((p) => `<kbd>${p}</kbd>`).join("")}<kbd>M</kbd></div>
        </div>
      </div>`;
    },
    bagla(el) {
      const ana = algi.ekranlar.find((e) => e.ana) || { w: 1920, h: 1080 }, ik = algi.ekranlar.find((e) => !e.ana);
      let n = 4;
      const e1 = $("#e1", el), e2 = $("#e2", el);
      const ciz = () => {
        const kap = durum.ikinciEkran && ik ? durum.tasma : 99;
        const a = Math.min(n, kap), b = n - a;
        karolariCiz(e1, a, e1.clientWidth, e1.clientHeight, ana.w, ana.h - 80, { dizilim: "auto", gelismis: false, bosluk: 8 });
        if (e2) karolariCiz(e2, Math.min(b, 3), e2.clientWidth, e2.clientHeight, ik.w, ik.h - 80, { dizilim: "auto", gelismis: false, bosluk: 8 });
      };
      anahtar(el, "#ikinci", durum.ikinciEkran, (v) => { ayarla({ ikinciEkran: v }); ciz(); });
      sayiBagla(el, ciz);
      ciz();
      return setInterval(() => { n = n >= durum.tasma + 3 ? 2 : n + 1; ciz(); }, 900);
    },
  },
  {
    id: "look", nav: "look",
    ciz: () => `
      <div class="gorsel"><div class="mini-ekran" id="l-ekran"></div></div>
      <div class="icerik" style="gap:12px">
        <h2>${t().lookT}</h2>
        <div class="tema-kartlar" id="temalar">${Object.keys(temalar).map((k) => `
          <button class="tema-kart" data-v="${k}"><div class="ornek" style="background:${temalar[k].zemin}"><i style="left:8%;background:${temalar[k].kart}"></i><i style="left:38%;background:${temalar[k].kart}"></i><i style="left:68%;background:${temalar[k].kart};outline:1.5px solid ${durum.vurgu}"></i></div>${t().th[k]}</button>`).join("")}</div>
        <div style="display:flex;align-items:center;justify-content:space-between;gap:12px">
          <div class="renkler" id="renkler"><span class="not" style="margin-right:4px">${t().accent}</span>${vurgular.map((c) => `<button class="renk" data-v="${c}" style="background:${c};color:${c}"></button>`).join("")}</div>
          <div class="segment" id="hiz">${["smooth", "fast", "off"].map((k) => `<button data-v="${k}">${t().sp[k]}</button>`).join("")}</div>
        </div>
        <div class="anahtar-satir" style="padding-bottom:0"><span class="metin">${t().fx}<small>${t().fxS}</small></span><button class="anahtar" id="efekt"></button></div>
      </div>`,
    bagla(el) {
      secim(el, "#temalar", durum.tema, (v) => { ayarla({ tema: v }); temaUygula(); }, ".tema-kart");
      secim(el, "#renkler", durum.vurgu, (v) => { ayarla({ vurgu: v }); temaUygula(); yenidenCiz(); }, ".renk");
      segment(el, "#hiz", hizAdi(), (v) => ayarla({ animasyonMs: hizlar[v] }));
      anahtar(el, "#efekt", durum.efekt, (v) => ayarla({ efekt: v }));
      karolariCiz($("#l-ekran", el), 3, 378, 184, 3840, 1972, { dizilim: "main", gelismis: false, anaOran: 0.55, bosluk: 6 }, ["izin", "calisiyor", "bitti"]);
    },
  },
  {
    id: "about", nav: "about", sadece: "ayarlar",
    ciz: () => `
      <div class="icerik" style="gap:12px">
        <h2>${t().aboutT} <span class="surum" style="font-family:var(--govde);font-size:13px;font-weight:450">${algi.surum}</span></h2>
        <p class="aciklama">${t().aboutD}</p>
        <div class="anahtar-satir"><span class="metin">${t().upd}<small>${algi.kurulu ? t().updS : t().devBuild}</small></span><button class="anahtar" id="gun"></button></div>
        <div style="display:flex;gap:8px;flex-wrap:wrap">
          ${algi.hazirSurum ? `<button class="kucuk-dugme" id="yeniden">${f(t().restartUpd, algi.hazirSurum)}</button>` : `<button class="kucuk-dugme" id="denetle" ${algi.kurulu ? "" : "disabled"}>${t().checkNow}</button>`}
          <button class="kucuk-dugme" id="sorun" title="${t().reportS}">${t().report}</button>
          <button class="kucuk-dugme" id="ayarKlasor">${t().openSettings}</button>
          <button class="kucuk-dugme" id="kayitKlasor">${t().openLogs}</button>
          <button class="kucuk-dugme" id="repo">${t().repo}</button>
        </div>
        <p class="not"><span>${f(t().ver, algi.surum)}</span></p>
      </div>`,
    bagla(el) {
      anahtar(el, "#gun", durum.guncelle, (v) => ayarla({ guncelle: v }));
      const tik = (s, m) => { const b = $(s, el); if (b) b.onclick = () => gonder(m); };
      tik("#denetle", { t: "guncelle" }); tik("#yeniden", { t: "yenidenBaslat" }); tik("#sorun", { t: "sorun" });
      tik("#ayarKlasor", { t: "klasorAc", ne: "ayar" }); tik("#kayitKlasor", { t: "klasorAc", ne: "kayit" });
      tik("#repo", { t: "link", url: "https://github.com/ofaruk3/clauding" });
    },
  },
  {
    id: "done", birincil: "finish", sadece: "onboarding",
    ciz: () => `
      <div class="gorsel"><div class="bitis-g"><div class="halka"><i></i></div></div></div>
      <div class="icerik" style="gap:10px">
        <h2>${t().doneT}</h2>
        <p class="aciklama">${t().doneD}</p>
        <dl class="ozet">
          <dt>${t().sum.shell}</dt><dd>${t().sh[durum.kabuk] || durum.kabuk} · ${durum.terminal === "wt" ? t().hostWT : t().hostCon}</dd>
          <dt>${t().sum.folder}</dt><dd>${(durum.klasorler[durum.varsayilan] || {}).yol || ""}${durum.klasorler.length > 1 ? ` +${durum.klasorler.length - 1}` : ""}</dd>
          <dt>${t().sum.keys}</dt><dd>${modKomb.replace(/\+/g, " + ")}</dd>
          <dt>${t().sum.layout}</dt><dd>${t().lay[durum.dizilim]}${durum.gelismis ? " + " + t().rules : ""}</dd>
        </dl>
        <div class="kontrol">
          <div class="anahtar-satir"><span class="metin">${t().login}</span><button class="anahtar" id="bas"></button></div>
          <div class="anahtar-satir"><span class="metin">${t().upd}<small>${t().updS}</small></span><button class="anahtar" id="gun"></button></div>
        </div>
      </div>`,
    bagla(el) {
      // default on for a first run: start with Windows
      if (!algi.baslangic && !el._ilk) { el._ilk = true; algi.baslangic = true; gonder({ t: "baslangic", acik: true }); }
      anahtar(el, "#bas", algi.baslangic, (v) => { algi.baslangic = v; gonder({ t: "baslangic", acik: v }); });
      anahtar(el, "#gun", durum.guncelle, (v) => ayarla({ guncelle: v }));
    },
  },
];

// ---------- deck (onboarding) / sidebar (settings) ----------
const deste = $("#deste");
let sira = 0, temizle = null, aktifEl = null;
const gorunur = () => kartlar.filter((k) => (!k.sadece || k.sadece === MOD) && (!k.kosul || k.kosul()));

function temizlik() { if (typeof temizle === "function") temizle(); else if (temizle) clearInterval(temizle); temizle = null; }

function goster(yeniSira, yon = 1) {
  const liste = gorunur();
  yeniSira = Math.max(0, Math.min(liste.length - 1, yeniSira));
  const kart = liste[yeniSira];
  temizlik();
  const eski = aktifEl;
  const el = html(`<section class="kart ${kart.sinif || ""} ${MOD === "onboarding" ? (yon > 0 ? "sag" : "sol") : ""}">${kart.ciz()}</section>`);
  deste.appendChild(el);
  el.getBoundingClientRect();
  el.classList.remove("sag", "sol");
  if (eski) { if (MOD === "onboarding") { eski.classList.add(yon > 0 ? "sol" : "sag"); setTimeout(() => eski.remove(), 650); } else eski.remove(); }
  aktifEl = el; sira = yeniSira;
  temizle = kart.bagla ? kart.bagla(el) : null;
  baglantilar(el);
  altCubuk();
  yanMenu();
  if (MOD === "ayarlar") deste.scrollTop = 0;
}

function yenidenCiz() {
  if (!aktifEl) return;
  const kart = gorunur()[sira];
  if (!kart) return goster(0);
  temizlik();
  const el = html(`<section class="kart ${kart.sinif || ""}">${kart.ciz()}</section>`);
  aktifEl.replaceWith(el); aktifEl = el;
  temizle = kart.bagla ? kart.bagla(el) : null;
  baglantilar(el);
  document.documentElement.lang = dil;
  altCubuk();
  yanMenu();
}

function baglantilar(el) { el.querySelectorAll("[data-url]").forEach((a) => a.addEventListener("click", () => gonder({ t: "link", url: a.dataset.url }))); }

function altCubuk() {
  if (MOD !== "onboarding") return;
  const liste = gorunur(), kart = liste[sira];
  $("#geri").hidden = sira === 0;
  $("#geri").textContent = t().back;
  $("#atla").textContent = t().skip;
  $("#atla").style.visibility = sira === liste.length - 1 ? "hidden" : "visible";
  const d = $("#devam");
  d.innerHTML = `${t()[kart.birincil || "next"]}<kbd>↵</kbd>`;
  d.classList.toggle("ana", !!kart.birincil);
  $("#ilerleme").innerHTML = liste.map((_, i) => `<i class="${i === sira ? "su" : i < sira ? "gecti" : ""}"></i>`).join("");
}

function yanMenu() {
  if (MOD !== "ayarlar") return;
  const liste = gorunur();
  $("#yan").innerHTML = `<div class="marka"><i></i>Clauding</div>` +
    liste.map((k, i) => `<button data-i="${i}" class="${i === sira ? "secili" : ""}">${t().nav[k.nav] || k.id}</button>`).join("") +
    `<div class="yan-alt">${f(t().ver, algi.surum)}</div>`;
  $("#yan").querySelectorAll("button[data-i]").forEach((b) => b.addEventListener("click", () => goster(+b.dataset.i)));
}

$("#devam").onclick = () => {
  const liste = gorunur();
  if (sira === liste.length - 1) { $("#devam").textContent = "✓"; gonder({ t: "bitir", ilkClaude: !!algi.claude }); return; }
  goster(sira + 1, 1);
};
$("#geri").onclick = () => goster(sira - 1, -1);
$("#atla").onclick = () => goster(gorunur().length - 1, 1);
$("#kapat").onclick = () => gonder({ t: "kapat" });
$("#kucult").onclick = () => gonder({ t: "kucult" });
$("#surukle").addEventListener("mousedown", (e) => { if (e.button === 0) gonder({ t: "tasi" }); });
addEventListener("keydown", (e) => {
  if (MOD !== "onboarding" || !$("#sahne").classList.contains("acik")) return;
  if (e.key === "Enter" && !e.target.closest("input, select")) { e.preventDefault(); $("#devam").click(); }
});

// ---------- intro (onboarding only) → cards ----------
const intro = $("#intro");
function introBitti() {
  if (intro.classList.contains("bitti")) return;
  intro.classList.add("bitti");
  setTimeout(() => { intro.src = "about:blank"; }, 1200);
  $("#sahne").classList.add("acik");
  if (!aktifEl) goster(0, 1);
}
addEventListener("message", (e) => { if (e.data === "ofc-intro-done") introBitti(); });

let sistemDili = "en", ilkDurum = true;
function durumAl(m) {
  sistemDili = m.dil || "en";
  durum = { ...durum, ...m.ayar };
  algi = { ...algi, ...m.algilanan };
  kisayol = m.kisayol || kisayol;
  dil = durum.dil === "auto" ? sistemDili : durum.dil;
  const y = kisayol.yeni || "Ctrl+Win+Alt+N";
  modKomb = y.split("+").slice(0, -1).join("+") || "Ctrl+Win+Alt";
  temaUygula();
  if (ilkDurum) {
    ilkDurum = false;
    if (MOD === "onboarding") { intro.src = "../intro/index.html"; setTimeout(introBitti, 12000); }
    else { $("#sahne").classList.add("acik"); goster(0); }
  } else if (aktifEl && !aktifEl.contains(document.activeElement)) yenidenCiz();
  else if (aktifEl && aktifEl._liste) aktifEl._liste();
}

if (host) {
  host.addEventListener("message", (e) => {
    const m = e.data;
    if (m.t === "durum") durumAl(m);
    else if (m.t === "hata") { const h = document.querySelector("#kancaHata"); if (h) { h.hidden = false; h.textContent = f(t().errHook, m.mesaj); } }
  });
  gonder({ t: "hazir" });
} else {
  // opened in a normal browser (design preview): demo state
  durumAl({ dil: "en", ayar: {}, algilanan: {} });
}
