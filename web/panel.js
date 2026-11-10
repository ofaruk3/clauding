"use strict";
// Shortcuts panel: every action with its keyboard shortcut (guide / record new ones) and the macro pad.

const host = window.chrome && window.chrome.webview;
const gonder = (m) => host && host.postMessage(m);
const $ = (id) => document.getElementById(id);

const S = {
  en: {
    baslik: "Shortcuts", altRehber: "Hold {0} to see this anytime", altDuzen: "Click a shortcut, then press the new keys",
    klavye: "Keyboard", makro: "Macro pad", rehber: "guide", duzen: "edit", yok: "—", basin: "press keys…",
    sifirla: "Reset to defaults", ayarlar: "Settings…", kaydedildi: "Saved", temizlendi: "Cleared",
    dolu: "{0} is taken by another app", tasindi: "Moved from “{0}”", ipucuKayit: "Esc cancels · Backspace clears",
    modGerek: "Use at least one of Ctrl, Alt or Win (F13–F24 work alone)",
    makroIpucu: "Click a key and choose its action. Drag keys onto each other to swap.",
    bir: "Choose a key", birAc: "Click a key or a knob on the pad.", suAn: "now: {0}", buTusta: "this key", yerDegisir: "F{0} · swaps",
    nobUst: "Top knob", nobAlt: "Bottom knob", nobAc: "Knobs are written into the pad itself: choose, then “upload to pad”.",
    sola: "↺ turning left", bas: "pressing", saga: "turning right ↻", secili: "chosen", yukle: "upload to pad",
    bekliyor: "saved, not on the pad yet", yukleniyor: "uploading…", yuklendi: "Uploaded to the pad", yuklenemedi: "Could not upload: {0}",
    yerDegisti: "Swapped",
  },
  tr: {
    baslik: "Kısayollar", altRehber: "Bunu görmek için istediğin an {0} basılı tut", altDuzen: "Bir kısayola tıkla, sonra yeni tuşlara bas",
    klavye: "Klavye", makro: "Makro klavye", rehber: "rehber", duzen: "düzenle", yok: "—", basin: "tuşlara bas…",
    sifirla: "Varsayılana dön", ayarlar: "Ayarlar…", kaydedildi: "Kaydedildi", temizlendi: "Temizlendi",
    dolu: "{0} başka bir uygulamada kayıtlı", tasindi: "“{0}” işinden alındı", ipucuKayit: "Esc iptal · Backspace temizler",
    modGerek: "Ctrl, Alt ya da Win'den en az biriyle kullan (F13–F24 tek başına olur)",
    makroIpucu: "Bir tuşa tıkla ve işini seç. Tuşları birbirinin üstüne sürükleyerek yer değiştirebilirsin.",
    bir: "Bir tuş seç", birAc: "Klavyeden bir tuşa ya da noba tıkla.", suAn: "şu an: {0}", buTusta: "bu tuşta", yerDegisir: "F{0} · yer değişir",
    nobUst: "Üst nob", nobAlt: "Alt nob", nobAc: "Noblar klavyenin içine yazılır: seç, sonra “klavyeye yükle”.",
    sola: "↺ sola çevirince", bas: "basınca", saga: "sağa çevirince ↻", secili: "seçili", yukle: "klavyeye yükle",
    bekliyor: "kaydedildi, klavyeye henüz yüklenmedi", yukleniyor: "yükleniyor…", yuklendi: "Klavyeye yüklendi", yuklenemedi: "Yüklenemedi: {0}",
    yerDegisti: "Yer değiştirdi",
  },
};
let dil = "en";
const T = (k, ...a) => { let s = (S[dil] || S.en)[k] || S.en[k] || k; a.forEach((v, i) => (s = s.replace("{" + i + "}", v))); return s; };

const IKON = {
  yeni: '<path d="M12 5v14M5 12h14"/>',
  bekleyen: '<path d="M6 16v-5a6 6 0 0 1 12 0v5l1.5 2h-15z"/><path d="M10 20.5a2 2 0 0 0 4 0"/>',
  izin: '<path d="M5 12.5l4.5 4.5L19 7"/>',
  durdur: '<rect x="6.5" y="6.5" width="11" height="11" rx="2.5"/>',
  buyut: '<path d="M14 4h6v6M10 20H4v-6M20 4l-6.5 6.5M4 20l6.5-6.5"/>',
  ayir: '<path d="M14 4h6v6M20 4l-8 8"/><path d="M17 14v4a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V9a2 2 0 0 1 2-2h4"/>',
  sola: '<path d="M19 12H5M11 6l-6 6 6 6"/>', saga: '<path d="M5 12h14M13 6l6 6-6 6"/>',
  alan: '<rect x="3.5" y="5" width="17" height="14" rx="2.5"/><path d="M12 5v14"/>',
  diz: '<rect x="4" y="4" width="7" height="7" rx="1.8"/><rect x="13" y="4" width="7" height="7" rx="1.8"/><rect x="4" y="13" width="7" height="7" rx="1.8"/><rect x="13" y="13" width="7" height="7" rx="1.8"/>',
  gizle: '<path d="M3 12s3.5-6 9-6 9 6 9 6-3.5 6-9 6-9-6-9-6z"/><circle cx="12" cy="12" r="2.5"/><path d="M4.5 4.5l15 15"/>',
  duraklat: '<path d="M9 6v12M15 6v12"/>',
  rehber: '<rect x="3" y="7" width="18" height="11" rx="2.5"/><path d="M7 11h.01M10.5 11h.01M14 11h.01M17.5 11h.01M8 14.5h8"/>',
  kapat: '<path d="M7 7l10 10M17 7 7 17"/>',
  ekran: '<rect x="2.5" y="4" width="13" height="9.5" rx="1.8"/><path d="M6.5 17h5M9 13.5V17"/><path d="M17 8h4.5M19.5 5.5 22 8l-2.5 2.5"/>',
  dizilim: '<rect x="3" y="4" width="10" height="16" rx="2"/><rect x="15" y="4" width="6" height="7" rx="1.5"/><rect x="15" y="13" width="6" height="7" rx="1.5"/>',
  bos: '<circle cx="12" cy="12" r="1.4"/>',
};
const ikonId = (id) => (IKON[id] ? id : id.startsWith("alan") ? "alan" : id.startsWith("dizilim") ? "dizilim" : id.startsWith("odak") || id.startsWith("git") ? "saga" : id.startsWith("yeni") || id === "klasorSec" ? "yeni" : "bos");
const svg = (id) => `<svg viewBox="0 0 24 24">${IKON[ikonId(id)]}</svg>`;

let d = null;          // state from the host
let mod = "rehber";
let sekme = "klavye";
let secili = null;     // macro pad: { tur: "tus", i } | { tur: "nob", ad }
let kayit = null;      // action id being recorded
let nobDegisti = false;

const eylem = (id) => d.eylemler.find((e) => e.id === id) || { id: "bos", ad: id, aciklama: "" };
const medya = (id) => (d.medya.find((m) => m.id === id) || { ad: id }).ad;

// ---------- render ----------
function ciz(ilk) {
  dil = d.dil || "en";
  document.documentElement.lang = dil;
  document.body.classList.toggle("duzen", mod === "duzen");
  document.body.classList.toggle("makro-sekme", sekme === "makro");
  $("pBaslik").textContent = T("baslik");
  $("pAlt").textContent = mod === "duzen" ? T("altDuzen") : T("altRehber", d.mod2 || "Ctrl+Win+Alt");
  $("sKlavye").textContent = T("klavye"); $("sMakro").textContent = T("makro");
  $("sekmeler").hidden = !d.makroVar;
  $("mRehber").textContent = T("rehber"); $("mDuzen").textContent = T("duzen");
  $("sifirla").textContent = T("sifirla"); $("ayarlarAc").textContent = T("ayarlar");
  $("altNot").textContent = kayit ? T("ipucuKayit") : "";
  document.querySelectorAll(".mod [data-mod]").forEach((b) => b.classList.toggle("secili", b.dataset.mod === mod));
  document.querySelectorAll(".sekmeler [data-sekme]").forEach((b) => b.classList.toggle("secili", b.dataset.sekme === sekme));
  $("kliste").hidden = sekme !== "klavye";
  $("makroSahne").hidden = sekme !== "makro";
  $("cekmece").hidden = sekme !== "makro";
  if (sekme === "klavye") listeyiCiz(ilk);
  else { tuslariCiz(ilk); noblariCiz(); cekmeceyiCiz(); $("ipucu").textContent = T("makroIpucu"); }
}

function parcala(s) {
  if (!s) return `<span class="bos-kisayol">${T("yok")}</span>`;
  return s.split("+").map((p) => `<kbd>${p}</kbd>`).join("");
}

function listeyiCiz(ilk) {
  const $l = $("kliste");
  const gruplar = d.gruplar.map((g) => ({ ...g, eylemler: d.eylemler.filter((e) => e.grup === g.id && e.id !== "bos") })).filter((g) => g.eylemler.length);
  $l.innerHTML = gruplar.map((g, gi) => `
    <div class="kgrup" style="animation-delay:${ilk ? gi * 40 : 0}ms">
      <h3>${g.ad}</h3>
      ${g.eylemler.map((e) => {
        const tus = d.klavye[e.id] || "";
        const hatali = tus && d.hatali.includes(tus);
        if (mod === "rehber" && !tus) return "";
        return `<div class="ksatir${kayit === e.id ? " kayitta" : ""}${hatali ? " hatali" : ""}" data-id="${e.id}" title="${e.aciklama || ""}">
          <span class="ik">${svg(e.id)}</span>
          <span class="ad">${e.ad}</span>
          <button class="kisayol" data-id="${e.id}">${kayit === e.id ? `<span class="basin">${T("basin")}</span>` : parcala(tus)}</button>
        </div>`;
      }).join("")}
    </div>`).join("");
  $l.querySelectorAll(".kisayol").forEach((b) => b.addEventListener("click", () => {
    if (mod !== "duzen") { modDegistir("duzen"); }
    kayitBasla(b.dataset.id);
  }));
}

// ---------- recording a shortcut ----------
const KOD = {
  ArrowLeft: "Left", ArrowRight: "Right", ArrowUp: "Up", ArrowDown: "Down", Comma: "Oemcomma", Period: "OemPeriod",
  BracketLeft: "OemOpenBrackets", BracketRight: "OemCloseBrackets", Enter: "Enter", NumpadEnter: "Enter", Escape: "Escape", Space: "Space",
  Minus: "OemMinus", Equal: "Oemplus", Slash: "OemQuestion", Semicolon: "OemSemicolon", Quote: "OemQuotes", Backslash: "OemPipe",
  Backquote: "Oemtilde", Tab: "Tab", Home: "Home", End: "End", PageUp: "PageUp", PageDown: "PageDown", Delete: "Delete", Insert: "Insert",
};
function tusAdi(e) {
  const c = e.code;
  if (/^Key[A-Z]$/.test(c)) return c.slice(3);
  if (/^Digit\d$/.test(c)) return c.slice(5);
  if (/^Numpad\d$/.test(c)) return "NumPad" + c.slice(6);
  if (/^F\d{1,2}$/.test(c)) return c;
  return KOD[c] || null;
}

function kayitBasla(id) {
  kayit = id;
  gonder({ t: "kayitBasla" });
  ciz(false);
}
function kayitBitir(gonderildi) {
  if (!kayit) return;
  if (!gonderildi) gonder({ t: "kayitBitti" });
  kayit = null;
  ciz(false);
}

document.addEventListener("keydown", (e) => {
  if (kayit) {
    e.preventDefault(); e.stopPropagation();
    if (["Control", "Alt", "Meta", "Shift", "OS"].includes(e.key)) return;
    if (e.code === "Escape" && !e.ctrlKey && !e.altKey && !e.metaKey) { kayitBitir(false); return; }
    if ((e.code === "Backspace" || e.code === "Delete") && !e.ctrlKey && !e.altKey && !e.metaKey) {
      gonder({ t: "kisayol", eylem: kayit, tus: "" }); bildir(T("temizlendi")); kayit = null; ciz(false); return;
    }
    const ad = tusAdi(e);
    if (!ad) return;
    const fYalniz = /^F(1[3-9]|2[0-4])$/.test(ad);
    if (!e.ctrlKey && !e.altKey && !e.metaKey && !fYalniz) { bildir(T("modGerek")); return; }
    const parca = [e.ctrlKey && "Ctrl", e.metaKey && "Win", e.altKey && "Alt", e.shiftKey && "Shift", ad].filter(Boolean);
    gonder({ t: "kisayol", eylem: kayit, tus: parca.join("+") });
    kayit = null;
    return;
  }
  if (e.key === "Escape") gonder({ t: "kapat" });
}, true);

// ---------- macro pad ----------
function tuslariCiz(ilk) {
  const $t = $("tuslar");
  $t.innerHTML = "";
  d.makro.forEach((id, i) => {
    const e = eylem(id);
    const b = document.createElement("button");
    b.className = "tus" + (id === "bos" ? " bos" : "") + (secili && secili.tur === "tus" && secili.i === i ? " secili" : "");
    b.style.animationDelay = ilk ? i * 28 + "ms" : "0ms";
    if (!ilk) b.style.animation = "none";
    b.innerHTML = `<span class="f">F${13 + i}</span>${svg(id)}<span class="ad">${e.ad}</span>`;
    b.title = e.aciklama;
    b.dataset.i = i;
    b.addEventListener("click", () => { if (mod !== "duzen") modDegistir("duzen"); secili = { tur: "tus", i }; ciz(false); });
    surukle(b, i);
    $t.appendChild(b);
  });
}

function noblariCiz() {
  const $n = $("noblar");
  $n.innerHTML = "";
  for (const ad of ["ust", "alt"]) {
    const k = document.createElement("div");
    k.className = "nob-kutu" + (secili && secili.tur === "nob" && secili.ad === ad ? " secili" : "");
    k.innerHTML = `
      <span class="nob-yazi sol"><b>${T("sola")}</b>${medya(d.nob[ad + "Sol"])}</span>
      <div class="nob"></div>
      <span class="nob-yazi"><b>${T("saga")}</b>${medya(d.nob[ad + "Sag"])}</span>
      <span class="nob-bas"><b>${T("bas")} · </b>${medya(d.nob[ad + "Bas"])}</span>`;
    k.addEventListener("click", () => { if (mod !== "duzen") modDegistir("duzen"); secili = { tur: "nob", ad }; ciz(false); });
    $n.appendChild(k);
  }
}

function cekmeceyiCiz() {
  const $c = $("cekmeceIc");
  if (!secili) { $c.innerHTML = `<div class="cek-baslik"><h2>${T("bir")}</h2><p>${T("birAc")}</p></div>`; return; }
  if (secili.tur === "tus") {
    const i = secili.i, simdiki = d.makro[i];
    let h = `<div class="cek-baslik"><h2>F${13 + i}</h2><p>${T("suAn", eylem(simdiki).ad)}</p></div>`;
    for (const e of d.eylemler) {
      const nerede = d.makro.indexOf(e.id);
      const yer = e.id === simdiki ? T("buTusta") : nerede >= 0 && e.id !== "bos" ? T("yerDegisir", 13 + nerede) : "";
      h += `<button class="secenek${e.id === simdiki ? " secili" : ""}" data-id="${e.id}">
        <span class="ik">${svg(e.id)}</span>
        <span><span class="ad">${e.ad}</span><span class="ac">${e.aciklama}</span></span>
        <span class="yer">${yer}</span></button>`;
    }
    $c.innerHTML = h;
    $c.querySelectorAll(".secenek").forEach((b) => b.addEventListener("click", () => ata(i, b.dataset.id)));
  } else {
    const ad = secili.ad;
    let h = `<div class="cek-baslik"><h2>${ad === "ust" ? T("nobUst") : T("nobAlt")}</h2><p>${T("nobAc")}</p></div>`;
    for (const [anahtar, baslik] of [["Sol", T("sola")], ["Bas", T("bas")], ["Sag", T("saga")]]) {
      h += `<div class="grup">${baslik}</div>`;
      for (const m of d.medya) {
        const sec = d.nob[ad + anahtar] === m.id;
        h += `<button class="secenek metin${sec ? " secili" : ""}" data-k="${ad + anahtar}" data-id="${m.id}"><span class="ad">${m.ad}</span><span class="yer">${sec ? T("secili") : ""}</span></button>`;
      }
    }
    h += `<button class="yukle" id="yukle"${nobDegisti ? "" : " disabled"}>${T("yukle")} <svg viewBox="0 0 16 16"><path d="M3 8h10M9 4l4 4-4 4"/></svg></button>
          <div class="durum-yazi" id="yukDurum">${nobDegisti ? `<span class="bekleyor">${T("bekliyor")}</span>` : ""}</div>`;
    $c.innerHTML = h;
    $c.querySelectorAll(".secenek").forEach((b) => b.addEventListener("click", () => {
      d.nob[b.dataset.k] = b.dataset.id; nobDegisti = true;
      gonder({ t: "nob", nob: { [b.dataset.k]: b.dataset.id } });
      ciz(false);
    }));
    $("yukle").addEventListener("click", () => {
      $("yukle").disabled = true; $("yukDurum").className = "durum-yazi"; $("yukDurum").textContent = T("yukleniyor");
      gonder({ t: "yukle" });
    });
  }
}

function ata(i, id) {
  const m = d.makro.slice();
  const eski = m[i], j = m.indexOf(id);
  if (j >= 0 && j !== i && id !== "bos") m[j] = eski;
  m[i] = id;
  d.makro = m;
  gonder({ t: "makro", makro: m });
  ciz(false);
  bildir(T("kaydedildi"));
}

function surukle(b, i) {
  b.draggable = true;
  b.addEventListener("dragstart", (e) => { e.dataTransfer.setData("text/plain", String(i)); b.classList.add("suruklenen"); });
  b.addEventListener("dragend", () => b.classList.remove("suruklenen"));
  b.addEventListener("dragover", (e) => { e.preventDefault(); b.classList.add("hedef"); });
  b.addEventListener("dragleave", () => b.classList.remove("hedef"));
  b.addEventListener("drop", (e) => {
    e.preventDefault(); b.classList.remove("hedef");
    const from = +e.dataTransfer.getData("text/plain");
    if (from === i) return;
    const m = d.makro.slice(); [m[from], m[i]] = [m[i], m[from]];
    d.makro = m; gonder({ t: "makro", makro: m });
    if (mod !== "duzen") modDegistir("duzen");
    secili = { tur: "tus", i }; ciz(false); bildir(T("yerDegisti"));
  });
}

let bildirimZ = 0;
function bildir(s, hata) {
  const b = $("bildirim"); b.textContent = s; b.classList.toggle("hata", !!hata); b.classList.add("gorunur");
  clearTimeout(bildirimZ); bildirimZ = setTimeout(() => b.classList.remove("gorunur"), hata ? 3200 : 1600);
}

function modDegistir(m) { if (kayit) kayitBitir(false); mod = m; if (m === "rehber") secili = null; ciz(false); }
document.querySelectorAll(".mod [data-mod]").forEach((b) => b.addEventListener("click", () => modDegistir(b.dataset.mod)));
document.querySelectorAll(".sekmeler [data-sekme]").forEach((b) => b.addEventListener("click", () => { if (kayit) kayitBitir(false); sekme = b.dataset.sekme; ciz(true); }));
$("kapat").addEventListener("click", () => { if (kayit) kayitBitir(false); gonder({ t: "kapat" }); });
$("sifirla").addEventListener("click", () => gonder({ t: "sifirla" }));
$("ayarlarAc").addEventListener("click", () => gonder({ t: "ayarlar" }));
$("tepe").addEventListener("mousedown", (e) => { if (!e.target.closest("button")) gonder({ t: "tasi" }); });

if (host) {
  host.addEventListener("message", (e) => {
    const m = e.data;
    if (m.t === "durum") {
      const yeniAcilis = !document.body.classList.contains("acik");
      d = m;
      if (yeniAcilis) { mod = m.mod; secili = null; nobDegisti = false; kayit = null; if (!m.makroVar) sekme = "klavye"; }
      document.body.classList.remove("kapaniyor");
      ciz(yeniAcilis);
      requestAnimationFrame(() => document.body.classList.add("acik"));
    } else if (m.t === "kayitSonuc") {
      if (m.sonuc === "dolu") bildir(T("dolu", m.tus), true);
      else if (m.sonuc && m.sonuc.startsWith("tasindi:")) bildir(T("tasindi", eylem(m.sonuc.slice(8)).ad));
      else bildir(m.tus ? T("kaydedildi") : T("temizlendi"));
    } else if (m.t === "kapaniyor") {
      if (kayit) kayitBitir(false);
      document.body.classList.add("kapaniyor"); document.body.classList.remove("acik");
    } else if (m.t === "odakKayip") {
      if (mod === "rehber" && document.body.classList.contains("acik")) gonder({ t: "kapat" });
    } else if (m.t === "yukleme") {
      nobDegisti = !m.ok;
      const s = $("yukDurum"); if (s) { s.className = "durum-yazi " + (m.ok ? "ok" : "hata"); s.textContent = m.ok ? T("yuklendi") : T("yuklenemedi", m.mesaj); }
      const b = $("yukle"); if (b) b.disabled = m.ok;
      if (m.ok) bildir(T("yuklendi"));
    }
  });
  gonder({ t: "hazir" });
} else {
  // browser preview: ?mod=duzen&dil=tr&makro=1
  const q = new URLSearchParams(location.search);
  const ad = (id) => id;
  d = {
    dil: q.get("dil") || "en", mod2: "Ctrl+Win+Alt", makroVar: q.has("makro"),
    makro: ["yeni", "bekleyen", "izin", "durdur", "buyut", "ayir", "sola", "saga", "alan", "diz", "gizle", "kapat"],
    klavye: { yeni: "Ctrl+Win+Alt+N", bekleyen: "Ctrl+Win+Alt+W", izin: "Ctrl+Win+Alt+Y", durdur: "Ctrl+Win+Alt+Esc", buyut: "Ctrl+Win+Alt+Enter", ayir: "Ctrl+Win+Alt+D", kapat: "Ctrl+Win+Alt+Q",
      odakSol: "Ctrl+Win+Alt+←", odakSag: "Ctrl+Win+Alt+→", odakYukari: "Ctrl+Win+Alt+↑", odakAsagi: "Ctrl+Win+Alt+↓", git1: "Ctrl+Win+Alt+1", git2: "Ctrl+Win+Alt+2",
      alanTam: "Ctrl+Win+Alt+F", alanSol: "Ctrl+Win+Alt+[", alanSag: "Ctrl+Win+Alt+]", dizilim: "Ctrl+Win+Alt+L", diz: "Ctrl+Win+Alt+T", rehber: "Ctrl+Win+Alt+K", ekran: "Ctrl+Win+Alt+M", klasorSec: "" },
    nob: { ustSol: "volumedown", ustBas: "play", ustSag: "volumeup", altSol: "prev", altBas: "mute", altSag: "next" },
    gruplar: [{ id: "claude", ad: "Claude" }, { id: "odak", ad: "Focus and order" }, { id: "pencere", ad: "Windows" }, { id: "alan", ad: "Work area" }, { id: "dizilim", ad: "Layout" }, { id: "uygulama", ad: "App" }],
    eylemler: [
      ["yeni", "claude", "New Claude"], ["klasorSec", "claude", "Folder picker"], ["bekleyen", "claude", "Go to waiting"], ["izin", "claude", "Approve"], ["durdur", "claude", "Interrupt"], ["kapat", "claude", "Close"],
      ["odakSol", "odak", "Focus left"], ["odakSag", "odak", "Focus right"], ["odakYukari", "odak", "Focus up"], ["odakAsagi", "odak", "Focus down"], ["git1", "odak", "Go to Claude 1"], ["git2", "odak", "Go to Claude 2"],
      ["buyut", "pencere", "Maximize"], ["ayir", "pencere", "Detach / attach"], ["diz", "pencere", "Re-tile"],
      ["alanTam", "alan", "Area: full screen"], ["alanSol", "alan", "Area: left half"], ["alanSag", "alan", "Area: right half"], ["ekran", "alan", "Other display"],
      ["dizilim", "dizilim", "Next layout"], ["rehber", "uygulama", "Shortcuts"],
    ].map(([id, grup, a]) => ({ id, grup, ad: ad(a), aciklama: "" })),
    medya: [{ id: "volumedown", ad: "Volume down" }, { id: "volumeup", ad: "Volume up" }, { id: "mute", ad: "Mute" }, { id: "play", ad: "Play / pause" }, { id: "prev", ad: "Previous" }, { id: "next", ad: "Next" }],
    hatali: [],
  };
  mod = q.get("mod") || "rehber";
  if (q.has("makro")) sekme = "makro";
  ciz(true);
  document.body.classList.add("acik");
}
